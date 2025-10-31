using System;
using System.Collections.Generic;
using racingGame;

public struct TimeAttackPlayer
{
    public int PlayerId {get; init;}

    public bool LocalPlayer {get; set;} = false;
    public bool InGame {get; set;} = true;

    public List<int> CheckPointsCollected {get; set;}
    public int LapsDone {get; set;} = 0;

    public DateTime RaceStartTime {get; set;}
    public DateTime SpawnTime {get; set;}
    public TimeSpan CurrentRaceTime {get; set;}
    public TimeSpan PbTime {get; set;}

    public Car PlayerCar {get; set;}

    public TimeAttackPlayer(int playerId, bool localPlayer, Car playerCar)
    {
        this.PlayerId = playerId;
        this.LocalPlayer = localPlayer;
        this.PlayerCar = playerCar;
    }
}