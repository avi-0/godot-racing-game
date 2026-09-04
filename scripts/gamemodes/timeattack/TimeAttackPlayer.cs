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
	public bool IsRespawning = false;
	public Vector3 RespawnLinearVelocity = Vector3.Zero;
	public Vector3 RespawnAngularVelocity = Vector3.Zero;
	public DateTime RespawnTime;
	
	[JsonIgnore]
	public Ghost PBGhost { get; set; } = new Ghost();
	[JsonIgnore]
	public Ghost GhostRecording { get; set; } = new Ghost();

	public string SplitText = "";
	public DateTime SplitTextChangeTime;
	
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
	public int TotalCheckPointsCollected { get; set; } = 0; //all laps
	
	public DateTime RaceStartTime { get; set; }
	public DateTime LapStartTime { get; set; }
	public DateTime SpawnTime { get; set; }
	public TimeSpan CurrentRaceTime { get; set; }
	public TimeSpan CurrentLapTime { get; set; }
	public TimeSpan PbTime { get; set; } = TimeSpan.Zero;
	public TimeSpan GlobalPbTime { get; set; } = TimeSpan.Zero;
	public TimeSpan RunFastestLapTime { get; set; } = TimeSpan.Zero;
	public TimeSpan GlobalFastestLapTime { get; set; } = TimeSpan.Zero;

	public bool HasFinished = false;
	public TimeSpan LastFinishTime;
	public int StartTimerSeconds = -1;
}