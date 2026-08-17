using System.Linq;
using Godot;

namespace racingGame;

public partial class GameModeController : Node
{
	public static IGameMode CurrentGameMode;

	public static int CurrentGameModeType;
	
	public static bool IsHost;
	
	public override void _Ready()
	{
		GameModeUtils.LaunchGameMode(GameModeUtils.GAMEMODE_TIMEATTACK);
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
		track.ResetPhysBlocks(false);

		CurrentGameMode.InitTrack(track);
	}

	public static void UnloadMap(Track track)
	{
		track.ResetPhysBlocks(true);
		
		CurrentGameMode.KillGame();
	}
}