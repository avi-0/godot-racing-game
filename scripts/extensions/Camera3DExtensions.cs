using Godot;

namespace racingGame;

public static class Camera3DExtensions
{
	public static void Match(this Camera3D camera, Camera3D other)
	{
		if (other == null)
			return;
		camera.GlobalTransform = other.GlobalTransform;
	}
}