using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using racingGame.data;
using racingGame.extensions;

namespace racingGame;

public class GameModeTimeAttack : IGameMode
{
	private TimeAttackMap _currentTrack;
	private bool _inEditor = false;
	private Dictionary<Guid, TimeAttackPlayer> _players;

	private bool _running = false;

	private bool _hasAuthor = false;

	public void Running(bool running)
	{
		_running = running;
	}

	public bool Running()
	{
		return _running;
	}

	public void Tick()
	{
		foreach (var playerId in _players.Keys)
		{
			var player = _players[playerId];

			if (player.InGame)
			{
				if (player.RaceStartTime.Ticks == 0)
				{
					var timeSinceStartMs = DateTime.Now.Subtract(player.SpawnTime).TotalMilliseconds;
					if (timeSinceStartMs > 1500)
					{
						player.StartTimerSeconds = 0;
						player.PlayerCar.AcceptsInputs = true;
						player.RaceStartTime = DateTime.Now;

						if (player.LocalPlayer && player.PlayerGhostCar != null)
						{
							player.PlayerGhostCar.Visible = true;
						}
					}
					else if (timeSinceStartMs > 1000)
					{
						player.StartTimerSeconds = 1;
					}
					else if (timeSinceStartMs > 500)
					{
						player.StartTimerSeconds = 2;
					}
					else
					{
						player.StartTimerSeconds = 3;
					}
				}
				else
				{
					player.CurrentRaceTime = DateTime.Now.Subtract(player.RaceStartTime);

					var ms = (int)player.CurrentRaceTime.TotalMilliseconds;
					var datanow = new CarPositionData(player.PlayerCar.Position, player.PlayerCar.Rotation);
					player.GhostRecording.AddFrame(ms, datanow);

					if (player.LocalPlayer && !player.PBGhost.Empty)
					{
						var data = player.PBGhost.GetFrame((int)player.CurrentRaceTime.TotalMilliseconds);
						player.PlayerGhostCar.Position = data.Position;
						player.PlayerGhostCar.Rotation = data.Rotation;
					}
				}
			}

			_players[playerId] = player;
		}
	}

	public void InitTrack(Track track)
	{
		_currentTrack = new TimeAttackMap(track);
		_players = new();

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

	public void AddPlayer(Guid id, bool localPlayer, bool isHost, string playerName)
	{
		var player = new TimeAttackPlayer(id, true);
		player.IsHost = isHost;
		player.PlayerName = playerName;
		player.LocalPlayer = localPlayer;

		if (player.LocalPlayer)
		{
			TimeSpan loadedPb = new TimeSpan();
			Ghost loadedGhost = new Ghost();
			if (!_inEditor)
			{
				if (!GameManager.Instance.IsSplitScreen || player.IsHost)
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
				player.PbTime = loadedPb;
			}

			if (!loadedGhost.Empty)
			{
				player.PBGhost = loadedGhost;
			}
		}

		_players[id] = player;
		RestartPlayer(id);
	}

	public void RestartPlayer(Guid id)
	{
		CarManager.Instance.CreatePlayerCar(id);
		
		var player = _players[id];

		player.SpawnTime = DateTime.Now;
		player.RaceStartTime = new DateTime();
		player.CurrentRaceTime = TimeSpan.Zero;
		player.CheckPointsCollected = new List<int>();
		player.LapsDone = 0;
		player.InGame = true;
		player.HasFinished = false;
		player.GhostRecording = new Ghost();
		player.RespawnPoint = new Transform3D();
		
		player.PlayerCar.IsLocallyControlled = player.LocalPlayer;
		if (player.LocalPlayer)
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
				player.PlayerGhostCar.SetGhost(true);
				player.PlayerGhostCar.Position = player.PlayerCar.Position;
				player.PlayerGhostCar.Rotation = player.PlayerCar.Rotation;
				player.PlayerGhostCar.Visible = false;
			}

			if (GameManager.Instance.IsSplitScreen && !player.IsHost)
			{
				player.PlayerCar.SetRandomSkin();
			}
		}

		if (_currentTrack.Track.Options.StartDayTime is <= 8 or >= 16)
		{
			player.PlayerCar.HeadLight.Visible = true;
		}

		player.PlayerCar.PlayerId = id;
		_players[id] = player;
	}

	public void RespawnPlayer(Guid id)
	{
		if (_players[id].RespawnPoint != new Transform3D())
		{
			_players[id].PlayerCar.TeleportToPoint(_players[id].RespawnPoint);
		}
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

	private void PlayerAttemptFinish(Car playerCar, int blockId)
	{
		if (playerCar.IsGhost) {return;}
		
		var playerId = playerCar.PlayerId;
		var player = _players[playerId];

		if (player.InGame && player.CheckPointsCollected.Count == _currentTrack.CheckPointCount)
		{
			player.LapsDone++;

			if (player.LapsDone < _currentTrack.Track.Options.Laps)
			{
				player.CheckPointsCollected = new List<int>();
				player.RespawnPoint = TrackManager.Instance.GetStartPoint();
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

		if (!player.CheckPointsCollected.Contains(blockId))
		{
			player.CheckPointsCollected.Add(blockId);
			if (player.LocalPlayer)
			{
				UiSoundPlayer.Singleton.CheckpointCollectedSound.Play();
			}
			
			player.RespawnPoint = player.PlayerCar.GetTransform(); // потом поменять на block.SpawnPoint (ещё вопрос как адекватно получать block из blockid)
		}

		_players[playerCar.PlayerId] = player;
	}

	private TimeAttackPlayer PlayerFinished(TimeAttackPlayer player)
	{
		player.InGame = false;
		player.PlayerCar.AcceptsInputs = false;

		var isPb = false;
		if (player.PbTime == TimeSpan.Zero ||
		    player.PbTime.TotalMilliseconds > player.CurrentRaceTime.TotalMilliseconds)
		{
			isPb = true;

			player.PbTime = player.CurrentRaceTime;
			if (player.LocalPlayer && (!GameManager.Instance.IsSplitScreen || player.IsHost))
			{
				GameModeUtils.SaveUserPb(player.PbTime, TrackManager.Instance.GetLoadedTrackUid());

				if (_inEditor) SetAuthorTime((int)player.CurrentRaceTime.TotalMilliseconds);
			}
		}

		//Ghost Saving
		if (player.PBGhost.Empty || player.CurrentRaceTime < player.PBGhost.RaceTime)
		{
			player.PBGhost = player.GhostRecording;
			player.PBGhost.RaceTime = player.CurrentRaceTime;

			if (player.LocalPlayer && (!GameManager.Instance.IsSplitScreen || player.IsHost))
			{
				GameModeUtils.SaveUserGhost(player.PBGhost, TrackManager.Instance.GetLoadedTrackUid());
			}
		}
		//--

		player.HasFinished = true;
		player.LastFinishTime = player.CurrentRaceTime;

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
		viewport.TimeLabel.Text = GameModeUtils.FormatRaceTime(player.CurrentRaceTime);
		viewport.CheckPointLabel.Text = GameModeUtils.FormatCheckPointCount(player.CheckPointsCollected.Count,
			_currentTrack.CheckPointCount);
		viewport.LapsLabel.Text = GameModeUtils.FormatLapsCount(player.LapsDone, _currentTrack.Track.Options.Laps);
		//--
			
		//ScoreBoard
		Label newLabel()
		{
			Label label = new Label();
			label.AddThemeFontSizeOverride("font_size", 48);
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
		
		foreach (var kv in _players)
		{
			var scoreboardPlayer = kv.Value;
			if (scoreboardPlayer.PbTime != TimeSpan.Zero)
			{
				Label scoreLabel = newLabel();
				scoreLabel.Text = scoreboardPlayer.PlayerName + ": " + GameModeUtils.FormatRaceTime(scoreboardPlayer.PbTime);
				scoreLabel.Name = scoreboardPlayer.PbTime.TotalMilliseconds.ToString("0000");
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


				
				int childId =  viewport.ScoreboardContainer.GetChildCount()-1;
				while (childId > 0 && int.Parse(viewport.ScoreboardContainer.GetChild(childId - 1).Name) > scoreboardPlayer.PbTime.TotalMilliseconds)
				{
					viewport.ScoreboardContainer.MoveChild(scoreLabel, childId-1);
					childId -= 1;
				}
				
				if (scoreboardPlayer.PbTime.TotalMilliseconds <= _currentTrack.Track.Options.AuthorTime)
				{
					_hasAuthor = true;
				}
			}
		}
		
		//--
		
		//Finish Panel
		if (player.HasFinished && !viewport.FinishPanel.Visible)
		{
			if (!viewport.RaceUi.Visible) {viewport.RaceUi.Visible = true;}
			
			var isPb = player.LastFinishTime == player.PbTime;
			
			viewport.FinishTimeLabel.Text = $"Race Time: {player.LastFinishTime:mm}:{player.LastFinishTime:ss}.{player.LastFinishTime:fff}";
			
			if (isPb)
			{
				if (!_inEditor)
					viewport.FinishTimeLabel.Text += "\nPersonal Best!!!";
				else
					viewport.FinishTimeLabel.Text += "\nNew Author Time!!!";
			}

			if (!_inEditor)
			{
				viewport.FinishTimeLabel.Text += "\n" + GameModeUtils.GetMedalFromTime((int)player.LastFinishTime.TotalMilliseconds, TrackManager.Instance.Track.Options.AuthorTime);
			}

			viewport.FinishPanel.Show();
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}
		//--
		
		//Start Sound
		if (viewport.StartTimerSeconds != player.StartTimerSeconds)
		{
			viewport.StartTimerSeconds = player.StartTimerSeconds;
			
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