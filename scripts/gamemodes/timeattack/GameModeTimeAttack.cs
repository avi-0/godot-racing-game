using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using racingGame.data;

namespace racingGame;

public class GameModeTimeAttack : IGameMode
{
	private TimeAttackMap _currentTrack;
	private bool _inEditor = false;
	private IList<TimeAttackPlayer> _players;

	private bool _running = false;

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
		for (var playerId = 0; playerId < _players.Count; playerId++)
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
		_players = new List<TimeAttackPlayer>();

		if (track.Options.Uid == "0")
			_inEditor = true;
		else
			_inEditor = false;

		GameManager.Singleton.SelectCarScene(track.Options.CarType);

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
		
		_currentTrack.Track.GetNode("Sky3D").GetNode("TimeOfDay").Set("current_time", (float)_currentTrack.Track.Options.StartDayTime);
		GameManager.Singleton.ApplyShadowSettings(); // FIXME: Sky3D слишком умный епт
	}

	public int SpawnPlayer(bool localPlayer, Car playerCar)
	{
		var playerId = _players.Count;
		_players.Add(new TimeAttackPlayer(playerId, localPlayer, playerCar));
		var player = _players[playerId];

		player.SpawnTime = DateTime.Now;
		player.CheckPointsCollected = new List<int>();

		player.PlayerCar.IsLocallyControlled = player.LocalPlayer;
		if (player.LocalPlayer)
		{
			TimeSpan loadedPb;
			if (!_inEditor)
				loadedPb = GameModeUtils.LoadUserPb(_currentTrack.Track.Options.Uid);
			else
				loadedPb = new TimeSpan(0, 0, 0, 0, _currentTrack.Track.Options.AuthorTime);

			if (loadedPb != TimeSpan.Zero)
			{
				player.PbTime = loadedPb;
			}
		}

		if (_currentTrack.Track.Options.StartDayTime is <= 8 or >= 16)
		{
			player.PlayerCar.HeadLight.Visible = true;
		}
		
		playerCar.PlayerId = playerId;
		_players[playerId] = player;
		return playerId;
	}

	public void RespawnPlayer(int playerId, Car playerCar)
	{
		var player = _players[playerId];
		
		player.PlayerCar = playerCar;
		player.SpawnTime = DateTime.Now;
		player.RaceStartTime = new DateTime();
		player.CheckPointsCollected = new List<int>();
		player.LapsDone = 0;
		player.InGame = true;
		player.HasFinished = false;
		player.GhostRecording = new Ghost();

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
				player.PlayerGhostCar = GameManager.Singleton.CreateCar();
				player.PlayerGhostCar.IsLocallyControlled = false;
				player.PlayerGhostCar.IsGhost = true;
				player.PlayerGhostCar.Position = player.PlayerCar.Position;
				//player.PlayerGhostCar.SetPlayerName(player.PBGhost.PlayerName); // почемуто ставит имя обоям машинам
			}
		}
		
		if (_currentTrack.Track.Options.StartDayTime is <= 8 or >= 16)
		{
			player.PlayerCar.HeadLight.Visible = true;
		}

		playerCar.PlayerId = playerId;
		_players[playerId] = player;
	}

	public void KillGame()
	{
		_running = false;
		foreach (TimeAttackPlayer player in _players)
		{
			if (player.PlayerGhostCar != null)
			{
				player.PlayerGhostCar.QueueFree();
			}
		}
		_players = null;
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
			if (player.LocalPlayer)
			{
				GameModeUtils.SaveUserPb(player.PbTime, GameManager.Singleton.GetLoadedTrackUid());

				if (_inEditor) SetAuthorTime((int)player.CurrentRaceTime.TotalMilliseconds);
			}

			player.PBGhost = player.GhostRecording;
		}

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
		var player = _players[viewport.Car.PlayerId];

		viewport.TrackInfoLabel.Text = GameModeUtils.FormatTrackInfo(_currentTrack.Track.Options.Name, _currentTrack.Track.Options.AuthorName);
		viewport.TimeLabel.Text = GameModeUtils.FormatRaceTime(player.CurrentRaceTime);
		viewport.PbLabel.Text = GameModeUtils.FormatPbTime(player.PbTime);
		viewport.CheckPointLabel.Text = GameModeUtils.FormatCheckPointCount(player.CheckPointsCollected.Count,
			_currentTrack.CheckPointCount);
		viewport.LapsLabel.Text = GameModeUtils.FormatLapsCount(player.LapsDone, _currentTrack.Track.Options.Laps);

		if (player.HasFinished && !viewport.FinishPanel.Visible)
		{
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
				var at = GameManager.Singleton.Track.Options.AuthorTime;
				if (player.LastFinishTime.TotalMilliseconds <= at)
					viewport.FinishTimeLabel.Text += "\nAuthor Medal!!!!";
				else if (player.LastFinishTime.TotalMilliseconds <= GameModeUtils.GetGoldFromAt(at))
					viewport.FinishTimeLabel.Text += "\nGold Medal!!!";
				else if (player.LastFinishTime.TotalMilliseconds <= GameModeUtils.GetSilverFromAt(at))
					viewport.FinishTimeLabel.Text += "\nSilver Medal!!";
				else if (player.LastFinishTime.TotalMilliseconds <= GameModeUtils.GetBronzeFromAt(at))
					viewport.FinishTimeLabel.Text += "\nBronze Medal!";
			}

			viewport.FinishPanel.Show();
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}
		
		if (viewport.StartTimerSeconds != player.StartTimerSeconds)
		{
			viewport.StartTimerSeconds = player.StartTimerSeconds;
			
			if (viewport.StartTimerSeconds == 0)
				UiSoundPlayer.Singleton.RaceStartSound.Play();
			else
				UiSoundPlayer.Singleton.RaceCountDownSound.Play();
		}

		if (viewport.StartTimerSeconds > 0)
		{
			viewport.StartTimerLabel.Show();
			viewport.StartTimerLabel.Text = viewport.StartTimerSeconds.ToString();
		}
		else
		{
			viewport.StartTimerLabel.Hide();
		}
	}
}