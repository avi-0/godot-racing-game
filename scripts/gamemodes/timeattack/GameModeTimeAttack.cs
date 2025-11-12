using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

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
						GameModeController.Utils.SetStartTimer(0);
						player.PlayerCar.AcceptsInputs = true;
						player.RaceStartTime = DateTime.Now;
					}
					else if (timeSinceStartMs > 1000)
					{
						GameModeController.Utils.SetStartTimer(1);
					}
					else if (timeSinceStartMs > 500)
					{
						GameModeController.Utils.SetStartTimer(2);
					}
					else
					{
						GameModeController.Utils.SetStartTimer(3);
					}
				}
				else
				{
					player.CurrentRaceTime = DateTime.Now.Subtract(player.RaceStartTime);
					if (player.LocalPlayer) GameModeController.Utils.UpdateLocalRaceTime(player.CurrentRaceTime);
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

		GameModeController.Utils.SetCheckPointCount(0, _currentTrack.CheckPointCount);
		GameModeController.Utils.SetTrackInfo(_currentTrack.Track.Options.Name, _currentTrack.Track.Options.AuthorName);
		GameModeController.Utils.SetLapsCount(0, _currentTrack.Track.Options.Laps);
		
		_currentTrack.Track.GetNode("Sky3D").GetNode("TimeOfDay").Set("current_time", (float)_currentTrack.Track.Options.StartDayTime);
	}

	public int SpawnPlayer(bool localPlayer, Car playerCar)
	{
		var playerId = _players.Count;
		_players.Add(new TimeAttackPlayer(playerId, localPlayer, playerCar));
		var player = _players[playerId];

		player.SpawnTime = DateTime.Now;
		player.CheckPointsCollected = new List<int>();

		if (player.LocalPlayer)
		{
			TimeSpan loadedPb;
			if (!_inEditor)
				loadedPb = GameModeController.Utils.LoadUserPb(_currentTrack.Track.Options.Uid);
			else
				loadedPb = new TimeSpan(0, 0, 0, 0, _currentTrack.Track.Options.AuthorTime);

			if (loadedPb != TimeSpan.Zero)
			{
				player.PbTime = loadedPb;
				GameModeController.Utils.UpdateLocalPb(player.PbTime);
			}
		}

		player.PlayerCar.IsLocallyControlled = true;

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

		if (player.LocalPlayer)
		{
			GameModeController.Utils.UpdateLocalRaceTime(TimeSpan.Zero);
			GameModeController.Utils.SetCheckPointCount(0, _currentTrack.CheckPointCount);
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
		_players = null;
		GameModeController.Utils.UnloadLocalStats();
	}

	private void PlayerAttemptFinish(Car playerCar, int blockId)
	{
		var playerId = playerCar.PlayerId;
		var player = _players[playerId];

		if (player.InGame && player.CheckPointsCollected.Count == _currentTrack.CheckPointCount)
		{
			player.LapsDone++;

			if (player.LapsDone < _currentTrack.Track.Options.Laps)
			{
				player.CheckPointsCollected = new List<int>();
				GameModeController.Utils.SetLapsCount(player.LapsDone, _currentTrack.Track.Options.Laps);
				GameModeController.Utils.SetCheckPointCount(0, _currentTrack.CheckPointCount);
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
		var player = _players[playerCar.PlayerId];

		if (!player.CheckPointsCollected.Contains(blockId))
		{
			player.CheckPointsCollected.Add(blockId);
			if (player.LocalPlayer)
			{
				GameModeController.Utils.SetCheckPointCount(player.CheckPointsCollected.Count,
					_currentTrack.CheckPointCount);
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
				GameModeController.Utils.UpdateLocalPb(player.PbTime);
				GameModeController.Utils.SaveUserPb(player.PbTime, GameManager.Singleton.GetLoadedTrackUid());

				if (_inEditor) SetAuthorTime((int)player.CurrentRaceTime.TotalMilliseconds);
			}
		}

		GameModeController.Utils.OpenFinishWindow(player.CurrentRaceTime, isPb, _inEditor);

		return player;
	}

	private void SetAuthorTime(int ms)
	{
		_currentTrack.Track.Options.AuthorTime = ms;
	}
}