using Godot;

namespace racingGame;

public partial class GameModeController : Node
{
	public static IGameMode CurrentGameMode;
	public static GameModeUtils Utils;

	public override void _Ready()
	{
		Utils = new GameModeUtils();

		Utils.TimeAttack();
	}

	public override void _Process(double delta)
	{
		if (CurrentGameMode.Running()) CurrentGameMode.Tick();
	}
}