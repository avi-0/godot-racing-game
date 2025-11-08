using Godot;

namespace racingGame;

public static class ViewportExtensions
{
	public static void MatchViewport(this Viewport viewport, Viewport baseViewport, bool aa = true)
	{
		viewport.Scaling3DScale = baseViewport.Scaling3DScale;

		if (aa)
		{
			viewport.ScreenSpaceAA = baseViewport.ScreenSpaceAA;
			viewport.UseTaa = baseViewport.UseTaa;
			viewport.Msaa3D = baseViewport.Msaa3D;
		}
	}
}