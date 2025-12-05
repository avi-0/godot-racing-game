using System;
using System.Collections.Generic;
using racingGame.data;

namespace racingGame;

public struct TimeAttackPlayer
{
	public int PlayerId { get; init; }

	public bool LocalPlayer { get; set; } = false;
	public bool InGame { get; set; } = true;

	public List<int> CheckPointsCollected { get; set; }
	public int LapsDone { get; set; } = 0;

	public DateTime RaceStartTime { get; set; }
	public DateTime SpawnTime { get; set; }
	public TimeSpan CurrentRaceTime { get; set; }
	public TimeSpan PbTime { get; set; }

	public Car PlayerCar { get; set; }
	public Car PlayerGhostCar { get; set; }

	public bool HasFinished = false;
	public TimeSpan LastFinishTime;
	public int StartTimerSeconds = -1;

	public Ghost PBGhost { get; set; } = new Ghost();
	public Ghost GhostRecording { get; set; } = new Ghost();
	
	public TimeAttackPlayer(int playerId, bool localPlayer, Car playerCar)
	{
		PlayerId = playerId;
		LocalPlayer = localPlayer;
		PlayerCar = playerCar;
	}
}