using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Godot;
using racingGame.data;

namespace racingGame;

public class TimeAttackPlayer : CartopiaPlayer
{
	public TimeAttackRaceData RaceData { get; set; } = new();
	
	public Transform3D RespawnPoint = new Transform3D();
	
	[JsonIgnore]
	public Car PlayerGhostCar { get; set; }
	[JsonIgnore]
	public Ghost PBGhost { get; set; } = new Ghost();
	[JsonIgnore]
	public Ghost GhostRecording { get; set; } = new Ghost();
	
	public TimeAttackPlayer(long playerId, int type) : base(playerId)
	{
		PlayerId = playerId;
		Type = type;
	}
}

public class TimeAttackRaceData
{
	public List<int> CheckPointsCollected { get; set; }
	public int LapsDone { get; set; } = 0;
	
	public DateTime RaceStartTime { get; set; }
	public DateTime SpawnTime { get; set; }
	public TimeSpan CurrentRaceTime { get; set; }
	public TimeSpan PbTime { get; set; } = TimeSpan.Zero;
	public TimeSpan GlobalPbTime { get; set; } = TimeSpan.Zero;

	public bool HasFinished = false;
	public TimeSpan LastFinishTime;
	public int StartTimerSeconds = -1;
}