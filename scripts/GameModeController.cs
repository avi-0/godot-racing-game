using Godot;

namespace racingGame;

public partial class GameModeController : Node
{
	public static IGameMode CurrentGameMode;

	public override void _Ready()
	{
		GameModeUtils.TimeAttack();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (CurrentGameMode.Running()) CurrentGameMode.Tick();
	}
}