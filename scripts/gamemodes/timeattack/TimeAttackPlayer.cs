using System;
using System.Collections.Generic;
using racingGame.data;

namespace racingGame;

public struct TimeAttackPlayer
{
	public Guid PlayerId { get; init; }

	public bool LocalPlayer { get; set; } = false;
	public bool InGame { get; set; } = true;

	public List<int> CheckPointsCollected { get; set; }
	public int LapsDone { get; set; } = 0;

	public DateTime RaceStartTime { get; set; }
	public DateTime SpawnTime { get; set; }
	public TimeSpan CurrentRaceTime { get; set; }
	public TimeSpan PbTime { get; set; }
	
	public Car PlayerGhostCar { get; set; }

	public bool HasFinished = false;
	public TimeSpan LastFinishTime;
	public int StartTimerSeconds = -1;

	public Ghost PBGhost { get; set; } = new Ghost();
	public Ghost GhostRecording { get; set; } = new Ghost();

	public Car PlayerCar => CarManager.Instance.GetPlayerCarById(PlayerId);
	
	public TimeAttackPlayer(Guid playerId, bool localPlayer)
	{
		PlayerId = playerId;
		LocalPlayer = localPlayer;
	}
}