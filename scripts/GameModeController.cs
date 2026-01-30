using System.Linq;
using Godot;

namespace racingGame;

public partial class GameModeController : Node
{
	public static IGameMode CurrentGameMode;

	public static bool IsHost;
	
	public override void _Ready()
	{
		GameModeUtils.TimeAttack();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (CurrentGameMode.Running())
		{
			CurrentGameMode.Tick();
			
			if (MultiplayerManager.Instance.OnServer)
			{
				MultiplayerManager.Instance.UpdateGameModeInfo();
			}
		}
	}

	public static void InitGameMode(bool Host)
	{
		IsHost = Host;
		CurrentGameMode.InitGameMode();
	}

	public static void LoadMap(Track track)
	{
		foreach (var block in track.FindChildren("*", "Block", false).Cast<Block>())
		{
			if (block.IsPhysical)
			{
				if (block.GetChild(0) is RigidBody3D rigidBody3D)
				{
					rigidBody3D.GlobalTransform = block.GlobalTransform;
					rigidBody3D.Freeze = false;
				}
			}
		}

		CurrentGameMode.InitTrack(track);
	}

	public static void UnloadMap(Track track)
	{
		foreach (var block in track.FindChildren("*", "Block", false).Cast<Block>())
		{
			if (block.IsPhysical)
			{
				if (block.GetChild(0) is RigidBody3D rigidBody3D)
				{
					rigidBody3D.GlobalTransform = block.GlobalTransform;
					rigidBody3D.Freeze = true;
				}
			}
		}
		
		CurrentGameMode.KillGame();
	}
}