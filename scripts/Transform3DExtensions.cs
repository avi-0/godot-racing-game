using Godot;

namespace racingGame;

public static class Transform3DExtensions
{
	public static Transform3D Rounded(this Transform3D transform)
	{
		for (int i = 0; i < 4; i++)
		{
			for (int j = 0; j < 3; j++)
			{
				transform[i, j] = Mathf.Snapped(transform[i, j], 1e-7f);
			}
		}

		return transform.Orthonormalized();
	}
}