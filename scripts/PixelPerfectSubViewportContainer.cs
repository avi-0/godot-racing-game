using Godot;
using System;

public partial class PixelPerfectSubViewportContainer : SubViewportContainer
{
	private bool DontRecurse = false;
	public override void _Ready()
	{
		Stretch = false;

		Resized += ResizeViewports;
		ResizeViewports();
	}

	private void ResizeViewports()
	{
		Stretch = false;
		
		if (DontRecurse)
			return;
		
		var sizeScale = GetViewport().GetStretchTransform().Scale;
		var scaledSize = (Vector2I) (Size * sizeScale).Round();
		//var window = GetWindow();
		//var viewportSize = (Vector2) window.Size;
		//var referenceSize = (Vector2) window.ContentScaleSize;
		//var viewportScale = viewportSize / referenceSize;
		//var sizeScale = Math.Min(viewportScale.X, viewportScale.Y);
		//var scaledSize = (Vector2I)(Size * Scale * sizeScale).Round();
		
		GD.Print(GetViewport().GetStretchTransform().Scale);
		GD.Print($"{Size} => {scaledSize}");

		DontRecurse = true;
		Scale = Vector2.One / sizeScale;
		GD.Print(Scale);
		Size = scaledSize;
		
		foreach (var child in GetChildren())
		{
			if (child is SubViewport subViewport)
			{
				subViewport.Size = scaledSize;
			}
		}
		
		DontRecurse = false;
	}
}
