using System;
using Godot;

namespace racingGame;

public interface IGameMode
{

	private struct GameModeInfoStruct;
	bool Running();
	void Running(bool running);
	void InitGameMode();
	void Tick();
	void InitTrack(Track track);
	void AddPlayer(long id, int playerType, string playerName); // creating a new player, only once per match
	void RestartPlayer(long id); // restarting existing player
	void RespawnPlayer(long id); // respawning existing player, if possible
	void DeletePlayer(long id);
	void KillGame(); // all gamemode variables will stay even after exiting the mode, so you have to clean up everything in this callback

	//server only
	string GetGameModeInfoJson(); // server sends gamemode status to all players
	void LoadGameModeInfoJson(string gameModeInfoJson); // players recieves fresh gamemode data from server
	//--
	
	void UpdateHud(PlayerViewport viewport);
}