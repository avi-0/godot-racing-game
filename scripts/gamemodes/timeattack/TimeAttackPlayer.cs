using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Godot;
using racingGame.data;

namespace racingGame;

public struct TimeAttackPlayer
{
	public long PlayerId { get; init; }
	
	public bool InGame { get; set; } = true;
	public int PlayerType {get; set;} = 0;
	public string PlayerName { get; set; } = "";
	
	public TimeAttackRaceData RaceData { get; set; } = new();
	
	public Transform3D RespawnPoint = new Transform3D();
	
	[JsonIgnore]
	public Car PlayerGhostCar { get; set; }
	[JsonIgnore]
	public Ghost PBGhost { get; set; } = new Ghost();
	[JsonIgnore]
	public Ghost GhostRecording { get; set; } = new Ghost();
	[JsonIgnore]
	public Car PlayerCar => CarManager.Instance.GetPlayerCarById(PlayerId);
	
	public TimeAttackPlayer(long playerId, int playerType)
	{
		PlayerId = playerId;
		PlayerType = playerType;
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

	public bool RaceOn { get; set; } = false;

	public bool HasFinished = false;
	public TimeSpan LastFinishTime;
	public int StartTimerSeconds = -1;
}