using System;
using Godot;

namespace racingGame;

public interface IGameMode
{
	bool Running();
	void Running(bool running);
	void Tick();
	void InitTrack(Track track);
	void AddPlayer(Guid id);
	void RestartPlayer(Guid id);
	void KillGame();

	void UpdateHud(PlayerViewport viewport);
}