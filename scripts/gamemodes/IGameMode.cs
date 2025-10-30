using Godot;

namespace racingGame;

public interface IGameMode
{
    bool Running();
    void Running(bool running);
    void Tick();
    void InitTrack(Node3D trackNode);
    int SpawnPlayer(bool localPlayer, Car playerCar);
    void RespawnPlayer(int playerId, Car playerCar);
    void PlayerAttemptFinish(int playerId);
    void KillGame();
}