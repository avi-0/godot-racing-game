using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Godot;
using Newtonsoft.Json;
using racingGame.data;
using racingGame.extensions;

namespace racingGame;

public class GameModeTimeAttack : IGameMode
{
	private TimeAttackMap _currentTrack;
	private bool _inEditor = false;
	private bool _running = false;
	private Dictionary<long, TimeAttackPlayer> _players { get; set;}
	
	private struct GameModeInfoStruct
	{
		public Dictionary<long, TimeAttackRaceData> PlayersRaceData { get; set;}
		public Dictionary<long, int> PlayerStates { get; set; }
		public int TimeLeft;
	}
	private GameModeInfoStruct _info;

	private bool _hasAuthor = false;
	
	public void Running(bool running)
	{
		_running = running;
	}

	public bool Running()
	{
		return _running;
	}

	public void InitGameMode()
	{
		_info = new GameModeInfoStruct();
		_players = new Dictionary<long, TimeAttackPlayer>();
	}
	
	public void Tick()
	{
		foreach (var playerId in _players.Keys)
		{
			var player = _players[playerId];

			if (player.State == GameModeUtils.PLAYER_STATE_PRESTART)
			{
				var timeSinceStartMs = DateTime.Now.Subtract(player.RaceData.SpawnTime).TotalMilliseconds;
				if (timeSinceStartMs > 1500)
				{
					player = PlayerStart(player);
				}
				else if (timeSinceStartMs > 1000)
				{
					player.RaceData.StartTimerSeconds = 1;
				}
				else if (timeSinceStartMs > 500)
				{
					player.RaceData.StartTimerSeconds = 2;
				}
				else
				{
					player.RaceData.StartTimerSeconds = 3;
				}
			}
			else if (player.State == GameModeUtils.PLAYER_STATE_PLAYING || player.State == GameModeUtils.PLAYER_STATE_DEAD)
			{
				player.RaceData.CurrentRaceTime = DateTime.Now.Subtract(player.RaceData.RaceStartTime);

				var ms = (int)player.RaceData.CurrentRaceTime.TotalMilliseconds;
				var datanow = new CarPositionData(player.PlayerCar.Position, player.PlayerCar.Rotation);
				player.GhostRecording.AddFrame(ms, datanow);

				if ((player.Type == GameModeUtils.PLAYER_LOCAL || player.Type == GameModeUtils.PLAYER_LOCAL_SPLITSCREEN) && !player.PBGhost.Empty)
				{
					var data = player.PBGhost.GetFrame((int)player.RaceData.CurrentRaceTime.TotalMilliseconds);
					player.PlayerGhostCar.Position = data.Position;
					player.PlayerGhostCar.Rotation = data.Rotation;
				}
			}

			_players[playerId] = player;
		}
		
		if (MultiplayerManager.Instance.OnServer && !MultiplayerManager.Instance.IsServer()) { return; }
		//server only ticks
	}

	public void InitTrack(Track track)
	{
		_currentTrack = new TimeAttackMap(track);

		if (track.Options.Uid == "0")
			_inEditor = true;
		else
			_inEditor = false;

		CarManager.Instance.SelectCarScene(track.Options.CarType);
		
		var blockCount = 0;
		foreach (var block in track.FindChildren("*", "Block", false).Cast<Block>())
		{
			block.BlockId = blockCount;
			blockCount++;

			if (block.IsCheckpoint)
			{
				_currentTrack.CheckPointCount++;
				block.CarEntered += PlayerEnterCheckPoint;
			}
			else if (block.IsFinish)
			{
				block.CarEntered += PlayerAttemptFinish;
			}
		}
	}

	public void AddPlayer(long id, int playerType, string playerName)
	{
		var player = new TimeAttackPlayer(id, playerType);
		player.PlayerName = playerName;

		if (player.Type == GameModeUtils.PLAYER_LOCAL || player.Type == GameModeUtils.PLAYER_LOCAL_SPLITSCREEN)
		{
			TimeSpan loadedPb = new TimeSpan();
			Ghost loadedGhost = new Ghost();
			if (!_inEditor)
			{
				if (player.Type == GameModeUtils.PLAYER_LOCAL)
				{
					loadedPb = GameModeUtils.LoadUserPb(_currentTrack.Track.Options.Uid);
					loadedGhost = GameModeUtils.LoadUserGhost(_currentTrack.Track.Options.Uid);
				}
			}
			else
			{
				loadedPb = new TimeSpan(0, 0, 0, 0, _currentTrack.Track.Options.AuthorTime);
			}

			if (loadedPb != TimeSpan.Zero)
			{
				player.RaceData.GlobalPbTime = loadedPb;
			}

			if (!loadedGhost.Empty)
			{
				player.PBGhost = loadedGhost;
			}
		}

		_players[id] = player;
		RestartPlayer(id);
	}

	public void RestartPlayer(long id)
	{
		CarManager.Instance.CreatePlayerCar(id);
		
		var player = _players[id];

		player.State = GameModeUtils.PLAYER_STATE_PRESTART;
		player.RaceData.SpawnTime = DateTime.Now;
		player.RaceData.RaceStartTime = new DateTime();
		player.RaceData.CurrentRaceTime = TimeSpan.Zero;
		player.RaceData.CheckPointsCollected = new List<int>();
		player.RaceData.LapsDone = 0;
		player.RaceData.HasFinished = false;
		
		player.GhostRecording = new Ghost();
		player.RespawnPoint = new Transform3D();
		
		player.PlayerCar.IsLocallyControlled = player.Type == GameModeUtils.PLAYER_LOCAL || player.Type == GameModeUtils.PLAYER_LOCAL_SPLITSCREEN;
		if (player.Type == GameModeUtils.PLAYER_LOCAL || player.Type == GameModeUtils.PLAYER_LOCAL_SPLITSCREEN)
		{
			if (player.PlayerGhostCar != null)
			{
				player.PlayerGhostCar.QueueFree();
				player.PlayerGhostCar = null;
			}

			if (!player.PBGhost.Empty)
			{
				player.PlayerGhostCar = CarManager.Instance.CreateCar();
				player.PlayerGhostCar.IsLocallyControlled = false;
				player.PlayerGhostCar.SetGhost(true, GameManager.Instance.GetPlayerViewPortById(player.PlayerId).CullLayer);
				player.PlayerGhostCar.Position = player.PlayerCar.Position;
				player.PlayerGhostCar.Rotation = player.PlayerCar.Rotation;
				player.PlayerGhostCar.Visible = false;
			}

			if (player.Type == GameModeUtils.PLAYER_LOCAL_SPLITSCREEN)
			{
				player.PlayerCar.SetRandomSkin();
			}
		}

		if (_currentTrack.Track.Options.StartDayTime is <= 8 or >= 16)
		{
			player.PlayerCar.InputToggleLights();
		}
		
		_players[id] = player;

		if (_players.Count == 1)
		{
			_currentTrack.Track.ResetPhysBlocks(false);
		}
	}

	public void RespawnPlayer(long id)
	{
		if ((_players[id].State == GameModeUtils.PLAYER_STATE_PLAYING || _players[id].State == GameModeUtils.PLAYER_STATE_DEAD) && _players[id].RespawnPoint != new Transform3D())
		{
			_players[id].State = GameModeUtils.PLAYER_STATE_PLAYING;
			_players[id].PlayerCar.TeleportToPoint(_players[id].RespawnPoint);
			_players[id].PlayerCar.CarModel.Show();
		}
	}

	public CartopiaPlayer GetPlayer(long id)
	{
		if (_players.ContainsKey(id))
		{
			return _players[id];
		}
		
		return null;
	}
	
	public void DeletePlayer(long id)
	{
		CarManager.Instance.RemoveChild(_players[id].PlayerCar);
		_players.Remove(id);
	}
	
	public void KillGame()
	{
		_running = false;
		foreach (TimeAttackPlayer player in _players.Values)
		{
			if (player.PlayerGhostCar != null)
			{
				player.PlayerGhostCar.QueueFree();
			}
		}
		_players = null;
		_hasAuthor = false;
	}

	public string GetGameModeInfoJson()
	{
		_info.PlayersRaceData = new Dictionary<long, TimeAttackRaceData>();
		_info.PlayerStates = new Dictionary<long, int>();
		foreach (var kv in _players)
		{
			_info.PlayersRaceData[kv.Key] = kv.Value.RaceData;
			_info.PlayerStates[kv.Key] = kv.Value.State;	
		}
		
		return JsonConvert.SerializeObject(_info);
	}

	public void LoadGameModeInfoJson(string gameModeInfoJson)
	{
		GameModeInfoStruct newInfo = JsonConvert.DeserializeObject<GameModeInfoStruct>(gameModeInfoJson);
		if (newInfo.PlayersRaceData != null)
		{
			foreach (var kv in newInfo.PlayersRaceData)
			{
				if (_players.ContainsKey(kv.Key))
				{
					TimeAttackPlayer player = _players[kv.Key];

					bool finished = player.Type == GameModeUtils.PLAYER_LOCAL && player.State == GameModeUtils.PLAYER_STATE_PLAYING && kv.Value.HasFinished;
					
					player.RaceData = kv.Value;
					player.State = newInfo.PlayerStates[kv.Key];
					
					if (finished)
					{
						player = PlayerFinished(player);
					}

					_players[kv.Key] = player;
				}
			}
			_info = newInfo;
		}
	}

	public TimeAttackPlayer PlayerStart(TimeAttackPlayer player)
	{
		player.State = GameModeUtils.PLAYER_STATE_PLAYING;
		player.RaceData.StartTimerSeconds = 0;
		player.RaceData.RaceStartTime = DateTime.Now;

		if ((player.Type == GameModeUtils.PLAYER_LOCAL || player.Type == GameModeUtils.PLAYER_LOCAL_SPLITSCREEN) && player.PlayerGhostCar != null)
		{
			player.PlayerGhostCar.Visible = true;

			if (player.Type == GameModeUtils.PLAYER_LOCAL && !SettingsManager.Instance.Settings.GhostVisible)
			{
				player.PlayerGhostCar.Visible = false;
			}
		}

		return player;
	}

	private void PlayerAttemptFinish(Car playerCar, int blockId)
	{
		if (playerCar == null || playerCar.IsGhost) {return;}
		if (MultiplayerManager.Instance.OnServer && !MultiplayerManager.Instance.IsServer()) {return;}
		
		var playerId = playerCar.PlayerId;
		if (playerId < 0 || !_players.ContainsKey(playerId)) {return;}
		var player = _players[playerId];

		if (player.State == GameModeUtils.PLAYER_STATE_PLAYING && player.RaceData.CheckPointsCollected.Count == _currentTrack.CheckPointCount)
		{
			player.RaceData.LapsDone++;

			if (player.RaceData.LapsDone < _currentTrack.Track.Options.Laps)
			{
				player.RaceData.CheckPointsCollected = new List<int>();
				
				Transform3D spawn = TrackManager.Instance.GetStartPoint();
				spawn.Origin = new Vector3(spawn.Origin.X, spawn.Origin.Y + playerCar.FrontWheelConfig.SpringRest + 0.1f, spawn.Origin.Z);
				player.RespawnPoint = spawn;
			}
			else
			{
				player = PlayerFinished(player);
			}
			
			UiSoundPlayer.Singleton.LapFinishedSound.Play();
		}

		_players[playerId] = player;
	}

	private void PlayerEnterCheckPoint(Car playerCar, int blockId)
	{
		if (playerCar.IsGhost) {return;}
		
		var player = _players[playerCar.PlayerId];

		if (!player.RaceData.CheckPointsCollected.Contains(blockId))
		{
			player.RaceData.CheckPointsCollected.Add(blockId);
			if (player.Type == GameModeUtils.PLAYER_LOCAL || player.Type == GameModeUtils.PLAYER_LOCAL_SPLITSCREEN)
			{
				UiSoundPlayer.Singleton.CheckpointCollectedSound.Play();
			}
			
			player.RespawnPoint = player.PlayerCar.GetTransform(); // потом поменять на block.SpawnPoint (ещё вопрос как адекватно получать block из blockid)
		}
		
		if (MultiplayerManager.Instance.OnServer && !MultiplayerManager.Instance.IsServer()) {return;}

		_players[playerCar.PlayerId] = player;
	}

	private TimeAttackPlayer PlayerFinished(TimeAttackPlayer player)
	{
		player.State = GameModeUtils.PLAYER_STATE_AFTERFINISH;

		var isPb = false;
		if (player.RaceData.PbTime == TimeSpan.Zero ||
			player.RaceData.PbTime.TotalMilliseconds > player.RaceData.CurrentRaceTime.TotalMilliseconds)
		{
			isPb = true;

			player.RaceData.PbTime = player.RaceData.CurrentRaceTime;
			if ((player.RaceData.GlobalPbTime == TimeSpan.Zero || player.RaceData.PbTime < player.RaceData.GlobalPbTime) && player.Type == GameModeUtils.PLAYER_LOCAL)
			{
				player.RaceData.GlobalPbTime = player.RaceData.PbTime;
				GameModeUtils.SaveUserPb(player.RaceData.PbTime, TrackManager.Instance.GetLoadedTrackUid());

				if (_inEditor) SetAuthorTime((int)player.RaceData.CurrentRaceTime.TotalMilliseconds);
			}
		}

		//Ghost Saving
		if (player.PBGhost.Empty || player.RaceData.CurrentRaceTime < player.PBGhost.RaceTime)
		{
			player.PBGhost = player.GhostRecording;
			player.PBGhost.RaceTime = player.RaceData.CurrentRaceTime;

			if (player.Type == GameModeUtils.PLAYER_LOCAL)
			{
				//separate thread to reduce game freeze while saving ghost to file
				Thread thread = new Thread(() => GameModeUtils.SaveUserGhost(player.PBGhost, TrackManager.Instance.GetLoadedTrackUid()));
				thread.Start();
			}
		}
		//--

		player.RaceData.HasFinished = true;
		player.RaceData.LastFinishTime = player.RaceData.CurrentRaceTime;

		return player;
	}

	private void SetAuthorTime(int ms)
	{
		_currentTrack.Track.Options.AuthorTime = ms;
	}

	public void UpdateHud(PlayerViewport viewport)
	{
		var player = _players[viewport.PlayerId];

		//Track Info
		viewport.TrackInfoLabel.Text = GameModeUtils.FormatTrackInfo(_currentTrack.Track.Options.Name, _currentTrack.Track.Options.AuthorName);
		//--
		
		//Race Stats
		viewport.TimeLabel.Text = GameModeUtils.FormatRaceTime(player.RaceData.CurrentRaceTime);
		viewport.CheckPointLabel.Text = GameModeUtils.FormatCheckPointCount(player.RaceData.CheckPointsCollected.Count,
			_currentTrack.CheckPointCount);
		viewport.LapsLabel.Text = GameModeUtils.FormatLapsCount(player.RaceData.LapsDone, _currentTrack.Track.Options.Laps);
		//--
			
		//ScoreBoard
		Label newLabel()
		{
			Label label = new Label();
			label.AddThemeFontSizeOverride("font_size", 24);
			label.HorizontalAlignment = HorizontalAlignment.Center;
			label.VerticalAlignment = VerticalAlignment.Center;
			label.Name = "Label";
			
			return label;
		}
		viewport.ScoreboardContainer.DestroyAllChildren();

		if (!_inEditor && _hasAuthor)
		{
			var authorLabel = newLabel();
			authorLabel.Text = "Author: " + GameModeUtils.FormatRaceTime(_currentTrack.Track.Options.AuthorTime);
			authorLabel.Name = _currentTrack.Track.Options.AuthorTime.ToString();
			authorLabel.AddThemeColorOverride("font_color", Colors.GreenYellow);
			viewport.ScoreboardContainer.AddChild(authorLabel);
		}
		var goldLabel = newLabel();
		goldLabel.Text = "Gold: " + GameModeUtils.FormatRaceTime(GameModeUtils.GetGoldFromAt(_currentTrack.Track.Options.AuthorTime));
		goldLabel.Name = GameModeUtils.GetGoldFromAt(_currentTrack.Track.Options.AuthorTime).ToString();
		goldLabel.AddThemeColorOverride("font_color", Colors.Gold);
		viewport.ScoreboardContainer.AddChild(goldLabel);
		var silverLabel = newLabel();
		silverLabel.Text = "Silver: " + GameModeUtils.FormatRaceTime(GameModeUtils.GetSilverFromAt(_currentTrack.Track.Options.AuthorTime));
		silverLabel.Name = GameModeUtils.GetSilverFromAt(_currentTrack.Track.Options.AuthorTime).ToString();
		silverLabel.AddThemeColorOverride("font_color", Colors.Silver);
		viewport.ScoreboardContainer.AddChild(silverLabel);
		var bronzeLabel = newLabel();
		bronzeLabel.Text = "Bronze: " + GameModeUtils.FormatRaceTime(GameModeUtils.GetBronzeFromAt(_currentTrack.Track.Options.AuthorTime));
		bronzeLabel.Name = GameModeUtils.GetBronzeFromAt(_currentTrack.Track.Options.AuthorTime).ToString();
		bronzeLabel.AddThemeColorOverride("font_color", Colors.LightCoral);
		viewport.ScoreboardContainer.AddChild(bronzeLabel);

		void moveChild(Label child, double ms)
		{
			int childId =  viewport.ScoreboardContainer.GetChildCount()-1;
			while (childId > 0 && int.Parse(viewport.ScoreboardContainer.GetChild(childId - 1).Name) > ms)
			{
				viewport.ScoreboardContainer.MoveChild(child, childId-1);
				childId -= 1;
			}
		}
		
		foreach (var kv in _players)
		{
			var scoreboardPlayer = kv.Value;
			
			if (scoreboardPlayer.Type == GameModeUtils.PLAYER_LOCAL && scoreboardPlayer.RaceData.GlobalPbTime != TimeSpan.Zero && (scoreboardPlayer.RaceData.GlobalPbTime < scoreboardPlayer.RaceData.PbTime || scoreboardPlayer.RaceData.PbTime == TimeSpan.Zero))
			{
				Label pbLabel = newLabel();
				pbLabel.Text = "[PB] " + scoreboardPlayer.PlayerName + ": " + GameModeUtils.FormatRaceTime(scoreboardPlayer.RaceData.GlobalPbTime);
				pbLabel.Name = scoreboardPlayer.RaceData.GlobalPbTime.TotalMilliseconds.ToString("0000");
				viewport.ScoreboardContainer.AddChild(pbLabel);
				viewport.ScoreboardContainer.MoveChild(pbLabel, 0);
				
				moveChild(pbLabel, scoreboardPlayer.RaceData.GlobalPbTime.TotalMilliseconds);

				if (scoreboardPlayer.RaceData.GlobalPbTime.TotalMilliseconds <= _currentTrack.Track.Options.AuthorTime)
				{
					_hasAuthor = true;
				}
			}
			
			if (scoreboardPlayer.RaceData.PbTime != TimeSpan.Zero)
			{
				Label scoreLabel = newLabel();
				scoreLabel.Text = scoreboardPlayer.PlayerName + ": " + GameModeUtils.FormatRaceTime(scoreboardPlayer.RaceData.PbTime);
				scoreLabel.Name = scoreboardPlayer.RaceData.PbTime.TotalMilliseconds.ToString("0000");
				if (kv.Key == viewport.PlayerId)
				{
					if (_inEditor)
					{
						scoreLabel.AddThemeColorOverride("font_color", Colors.LightGreen);
					}
					else
					{
						scoreLabel.AddThemeColorOverride("font_color", Colors.LightSkyBlue);
					}
				}
				viewport.ScoreboardContainer.AddChild(scoreLabel);
				
				moveChild(scoreLabel, scoreboardPlayer.RaceData.PbTime.TotalMilliseconds);
				
				if (scoreboardPlayer.RaceData.PbTime.TotalMilliseconds <= _currentTrack.Track.Options.AuthorTime)
				{
					_hasAuthor = true;
				}
			}
		}
		
		//--
		
		//Finish Panel
		if (player.State == GameModeUtils.PLAYER_STATE_AFTERFINISH && !viewport.FinishPanel.Visible)
		{
			if (!viewport.RaceUi.Visible) {viewport.RaceUi.Visible = true;}
			
			var isPb = player.RaceData.LastFinishTime == player.RaceData.GlobalPbTime;
			
			viewport.FinishTimeLabel.Text = $"Race Time: {player.RaceData.LastFinishTime:mm}:{player.RaceData.LastFinishTime:ss}.{player.RaceData.LastFinishTime:fff}";
			
			if (isPb)
			{
				if (!_inEditor)
					viewport.FinishTimeLabel.Text += "\nPersonal Best!!!";
				else
					viewport.FinishTimeLabel.Text += "\nNew Author Time!!!";
			}

			if (!_inEditor)
			{
				viewport.FinishTimeLabel.Text += "\n" + GameModeUtils.GetMedalFromTime((int)player.RaceData.LastFinishTime.TotalMilliseconds, TrackManager.Instance.Track.Options.AuthorTime);
			}
			
			viewport.FinishTimeLabel.Text += "\n Press [img=32x32]res://assets/icons/controls/game_restart.tres[/img] to restart";
			
			viewport.FinishPanel.Show();
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}
		else if (player.State != GameModeUtils.PLAYER_STATE_AFTERFINISH && viewport.FinishPanel.Visible)
		{
			viewport.FinishPanel.Hide();
			Input.MouseMode = Input.MouseModeEnum.Captured;
		}
		//--
		
		//Start Sound
		if (viewport.StartTimerSeconds != player.RaceData.StartTimerSeconds)
		{
			viewport.StartTimerSeconds = player.RaceData.StartTimerSeconds;
			
			if (viewport.StartTimerSeconds == 0)
				UiSoundPlayer.Singleton.RaceStartSound.Play();
			else
				UiSoundPlayer.Singleton.RaceCountDownSound.Play();
		}
		//--

		//Start CountDown
		if (viewport.StartTimerSeconds > 0)
		{
			viewport.StartTimerLabel.Show();
			viewport.StartTimerLabel.Text = viewport.StartTimerSeconds.ToString();
		}
		else
		{
			viewport.StartTimerLabel.Hide();
		}
		//--
	}
}
