using Godot;

namespace racingGame;

public static class ViewportExtensions
{
	public static void MatchViewport(this Viewport viewport, Viewport baseViewport)
	{
		viewport.Scaling3DScale = baseViewport.Scaling3DScale;
		viewport.ScreenSpaceAA = baseViewport.ScreenSpaceAA;
		viewport.UseTaa = baseViewport.UseTaa;
		viewport.Msaa3D = baseViewport.Msaa3D;
	}
}