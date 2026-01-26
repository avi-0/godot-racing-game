using Godot;
using System;
using racingGame;

public partial class MultiplayerSpawner : Godot.MultiplayerSpawner
{
	public override void _Ready()
	{
		Multiplayer.PeerConnected += SpawnPlayer;
	}
	
	public override void _Process(double delta)
	{
	}

	public void SpawnPlayer(long id)
	{
		if (!Multiplayer.IsServer() || !GameModeController.CurrentGameMode.Running()) {return;}
		
		MultiplayerManager.Instance.OnClientConnected(id);
		
		GameModeController.CurrentGameMode.AddPlayer(id, false, false, id.ToString());
	}
}
