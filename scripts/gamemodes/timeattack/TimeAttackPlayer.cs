using System;
using racingGame;

public struct TimeAttackPlayer
{
    public int PlayerId {get; init;}

    public bool LocalPlayer {get; set;} = false;
    public bool InGame {get; set;} = true;

    public int CheckPointsCollected {get; set;} = 0;
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