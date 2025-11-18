using Godot;

namespace racingGame;

public static class InputActionNames
{
	public static readonly StringName Forward = new("game_forward");
	public static readonly StringName Back = new("game_back");
	public static readonly StringName Left = new("game_left");
	public static readonly StringName Right = new("game_right");
	public static readonly StringName Restart = new("game_restart");
	public static readonly StringName CycleCamera = new("game_cycle_camera");
	public static readonly StringName ToggleLights = new("game_car_lights");
	public static readonly StringName Pause = new("game_pause");
}