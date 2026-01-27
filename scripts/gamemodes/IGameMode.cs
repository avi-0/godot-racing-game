using System;
using Godot;

namespace racingGame;

public interface IGameMode
{
	bool Running();
	void Running(bool running);
	void Tick();
	void InitTrack(Track track);
	void AddPlayer(long id, bool localPlayer, bool isHost, string playerName); // creating a new player, only once per match
	void RestartPlayer(long id); // restarting existing player
	void RespawnPlayer(long id); // respawning existing player, if possible
	void DeletePlayer(long id);
	void KillGame(); // all gamemode variables will stay even after exiting the mode, so you have to clean up everything in this callback

	void UpdateHud(PlayerViewport viewport);
}