using Godot;

namespace racingGame;

public static class InputActionNames
{
	public static StringName Forward = new StringName("game_forward");
	public static StringName Back = new StringName("game_back");
	public static StringName Left = new StringName("game_left");
	public static StringName Right = new StringName("game_right");
	public static StringName Restart = new StringName("game_restart");
	public static StringName CycleCamera = new StringName("game_cycle_camera");
	public static StringName ToggleLights = new StringName("game_car_lights");
}