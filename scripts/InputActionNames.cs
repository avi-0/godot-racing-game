using Godot;

namespace racingGame;

public static class InputActionNames
{
	public static StringName Forward = new("game_forward");
	public static StringName Back = new("game_back");
	public static StringName Left = new("game_left");
	public static StringName Right = new("game_right");
	public static StringName Restart = new("game_restart");
	public static StringName CycleCamera = new("game_cycle_camera");
	public static StringName ToggleLights = new("game_car_lights");
}