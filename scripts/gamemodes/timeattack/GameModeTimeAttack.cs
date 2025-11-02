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

	public void InitTrack(Node3D trackNode)
	{
		_currentTrack = new TimeAttackMap(GameManager.Singleton.CurrentTrackMeta["TrackUID"], trackNode);
		_players = new List<TimeAttackPlayer>();

		if (_currentTrack.TrackUid == "0")
			_inEditor = true;
		else
			_inEditor = false;

		if (GameManager.Singleton.CurrentTrackMeta.ContainsKey("AuthorTime"))
			_currentTrack.AuthorTime = GameManager.Singleton.CurrentTrackMeta["AuthorTime"].ToInt();
		else
			_currentTrack.AuthorTime = 0;

		GameManager.Singleton.SelectCarScene(GameManager.Singleton.CurrentTrackMeta["CarType"]);

		var blockCount = 0;
		foreach (var block in trackNode.FindChildren("*", "Block", false).Cast<Block>())
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
				//block.CarEntered += PlayerAttemptFinish;
			}
		}

		GameModeController.Utils.SetCheckPointCount(0, _currentTrack.CheckPointCount);

		_currentTrack.MapType = GameManager.Singleton.CurrentTrackMeta["MapType"];
		_currentTrack.TrackName = GameManager.Singleton.CurrentTrackMeta["TrackName"];
		_currentTrack.AuthorName = GameManager.Singleton.CurrentTrackMeta["AuthorName"];
		GameModeController.Utils.SetTrackInfo(_currentTrack.TrackName, _currentTrack.AuthorName);

		_currentTrack.LapsCount = GameManager.Singleton.CurrentTrackMeta["LapsCount"].ToInt();
		GameModeController.Utils.SetLapsCount(0, _currentTrack.LapsCount);
	}

	public int SpawnPlayer(bool localPlayer, NewCar playerCar)
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
				loadedPb = GameModeController.Utils.LoadUserPb(_currentTrack.TrackUid);
			else
				loadedPb = new TimeSpan(0, 0, 0, 0, _currentTrack.AuthorTime);

			if (loadedPb != TimeSpan.Zero)
			{
				player.PbTime = loadedPb;
				GameModeController.Utils.UpdateLocalPb(player.PbTime);
			}
		}

		playerCar.PlayerId = playerId;
		_players[playerId] = player;
		return playerId;
	}

	public void RespawnPlayer(int playerId, NewCar playerCar)
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

		playerCar.PlayerId = playerId;
		_players[playerId] = player;
	}

	public void KillGame()
	{
		_running = false;
		_players = null;
		GameModeController.Utils.UnloadLocalStats();
	}

	private void PlayerAttemptFinish(NewCar playerCar, int blockId)
	{
		var playerId = playerCar.PlayerId;
		var player = _players[playerId];

		if (player.InGame && player.CheckPointsCollected.Count == _currentTrack.CheckPointCount)
		{
			player.LapsDone++;

			if (player.LapsDone < _currentTrack.LapsCount)
			{
				player.CheckPointsCollected = new List<int>();
				GameModeController.Utils.SetLapsCount(player.LapsDone, _currentTrack.LapsCount);
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
		_currentTrack.AuthorTime = ms;
		GameManager.Singleton.CurrentTrackMeta["AuthorTime"] = ms.ToString();
	}
}