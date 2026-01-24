using System.Collections.Generic;
using Godot;
using racingGame.data;

namespace racingGame;

public partial class Track : Node3D
{
	[Export] public Node TimeOfDay;
	[Export] public DirectionalLight3D Sun;
	[Export] public DirectionalLight3D Moon;
	[Export] public VoxelGI TrackVoxelGI;
	[Export] public GpuParticles3D RainParticles;
	
	public TrackOptions Options = new();

	public void UpdateLighting()
	{
		TimeOfDay.Set("current_time", Options.StartDayTime);
		TimeOfDay.Call("_update_celestial_coords"); // make sure to reenable shadows as needed
		if (!SettingsManager.Instance.DirectionalShadowsEnabled)
		{
			Sun.ShadowEnabled = false;
			Moon.ShadowEnabled = false;
		}
		
		TimeOfDay.GetParent<WorldEnvironment>().Environment.VolumetricFogEnabled = Options.Fog;
		RainParticles.Visible = Options.Rain;
	}
	
	public TrackData Save()
	{
		var data = new TrackData();

		data.Blocks = new List<BlockPlacementData>();
		foreach (var child in GetChildren())
		{
			if (child is Block block)
			{
				data.Blocks.Add(block.Save());
			}
		}

		data.Options = Options;

		return data;
	}

	public void Load(TrackData data)
	{
		foreach (var child in GetChildren())
		{
			if (child is Block)
			{
				RemoveChild(child);
				child.QueueFree();
			}
		}

		foreach (var blockPlacementData in data.Blocks)
		{
			var block = Block.Load(blockPlacementData);
			AddChild(block);
			
			block.Owner = this; // not needed but helps for FindChildren etc
		}

		Options = data.Options;
		
		UpdateLighting();
	}
}
