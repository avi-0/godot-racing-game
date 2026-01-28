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
}