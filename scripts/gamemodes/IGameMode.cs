using Godot;

namespace racingGame;

public interface IGameMode
{
	bool Running();
	void Running(bool running);
	void Tick();
	void InitTrack(Node3D trackNode);
	int SpawnPlayer(bool localPlayer, NewCar playerCar);
	void RespawnPlayer(int playerId, NewCar playerCar);
	void KillGame();
}