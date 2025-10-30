using System;
using System.Collections.Generic;
using Godot;

namespace racingGame;

public class GameModeTimeAttack : IGameMode
{
    private IList<TimeAttackPlayer> _players;
    private TimeAttackMap _currentTrack;

    private bool _running = false;
    public void Running(bool running) {this._running = running;}
    public bool Running() {return _running;}

    public void Tick()
    {
        for (int playerId = 0; playerId < _players.Count; playerId++)
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
                    if (player.LocalPlayer)
                    {
                        GameModeController.Utils.UpdateLocalRaceTime(player.CurrentRaceTime);
                    }
                }
            }

            _players[playerId] = player;
        }	
    }

    public void InitTrack(Node3D trackNode)
    {
        _currentTrack = new TimeAttackMap(trackNode);
        _players = new List<TimeAttackPlayer>();
    }

    public int SpawnPlayer(bool localPlayer, Car playerCar)
    {
        var playerId = _players.Count;
        _players.Add(new TimeAttackPlayer(playerId, localPlayer, playerCar));
        var player = _players[playerId];

        player.SpawnTime = DateTime.Now;

        _players[playerId] = player;
        return playerId;
    }

    public void RespawnPlayer(int playerId, Car playerCar)
    {
        var player = _players[playerId];

        player.PlayerCar = playerCar;
        player.SpawnTime = DateTime.Now;
        player.RaceStartTime = new DateTime();
        player.InGame = true;

        if (player.LocalPlayer)
        {
            GameModeController.Utils.UpdateLocalRaceTime(TimeSpan.Zero);
        }

        _players[playerId] = player;
    }

    public void PlayerAttemptFinish(int playerId)
    {
        if (_players[playerId].InGame && _players[playerId].CheckPointsCollected == _currentTrack.CheckPointCount)
        {
            PlayerFinished(playerId);
        }
    }	

    public void KillGame()
    {
        _running = false;
        _players = null;	
        GameModeController.Utils.UnloadLocalPb();
    }

    private void PlayerFinished(int playerId)
    {
        var player = _players[playerId];

        player.InGame = false;
        player.PlayerCar.AcceptsInputs = false;


        var isPb = false;
        if (player.PbTime.TotalMilliseconds == 0 || TimeSpan.Compare(player.PbTime, player.CurrentRaceTime) == 1)
        {
            isPb = true;

            player.PbTime = player.CurrentRaceTime;
            if (player.LocalPlayer)
            {
                GameModeController.Utils.UpdateLocalPb(player.PbTime);
            }
        }
        GameModeController.Utils.OpenFinishWindow(player.CurrentRaceTime, isPb);

        _players[playerId] = player;
    }
}