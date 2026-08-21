using System.Collections.Generic;
using System.Linq;
using Godot;
using racingGame.data;

namespace racingGame;

public partial class Track : Node3D
{
	[Export] public Node TimeOfDay;
	[Export] public DirectionalLight3D Sun;
	[Export] public DirectionalLight3D Moon;
	[Export] public VoxelGI TrackVoxelGI;
	
	public TrackOptions Options = new();

	public PackedScene FeScene;
	
	public Node3D TrackBase;
	
	public void UpdateLighting() // also called when user updates graphics settings
	{
		TimeOfDay.Set("current_time", Options.StartDayTime);
		TimeOfDay.Call("_update_celestial_coords"); // make sure to reenable shadows as needed
		if (!SettingsManager.Instance.DirectionalShadowsEnabled)
		{
			Sun.ShadowEnabled = false;
			Moon.ShadowEnabled = false;
		}

		TimeOfDay.GetParent<WorldEnvironment>().Environment.VolumetricFogEnabled = Options.Fog;

		TimeOfDay.GetParent<WorldEnvironment>().Environment.SsaoEnabled = SettingsManager.Instance.Settings.Graphics.Ao == 1;
		TimeOfDay.GetParent<WorldEnvironment>().Environment.SsrEnabled = SettingsManager.Instance.Settings.Graphics.Reflection == 1;
		
		foreach (var child in GetChildren())
		{
			if (child is Block block && block.HasLight)
			{
				foreach (var blockChild in block.GetChild(0).GetChildren())
				{
					if (blockChild is SpotLight3D light)
					{
						light.ShadowEnabled = SettingsManager.Instance.DirectionalShadowsEnabled;
					}
				}
			}
		}
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

			if (block != null)
			{
				AddChild(block);

				block.Owner = this; // not needed but helps for FindChildren etc

				if ((Node3D)block.GetChild(0).GetChild(0) is MeshInstance3D mesh)
				{
					mesh.ExtraCullMargin = 4;
				}
			}
		}

		Options = data.Options;

		ReloadTrackBase();
		
		UpdateLighting();
	}

	public void ReloadTrackBase()
	{
		if (TrackBase != null)
		{
			RemoveChild(TrackBase);
			TrackBase.QueueFree();
		}

		var path = "res://scenes/game/track_bases/track_base_" + Options.TrackBase + ".tscn";
		PackedScene baseScene = (PackedScene)GD.Load(path);
		TrackBase = baseScene.Instantiate<Node3D>();
		TrackBase.SetName("TrackBase");
		AddChild(TrackBase, true);
		
		FeScene = GD.Load<PackedScene>("res://scenes/game/follow_effects/" + Options.TrackBase + "_fe.tscn");
	}

	public void ResetPhysBlocks(bool freeze)
	{
		foreach (var child in GetChildren())
		{
			if (child is Block block && block.IsPhysical)
			{
				if (block.GetChild(0) is RigidBody3D rigidBody3D)
				{
					rigidBody3D.AngularVelocity = Vector3.Zero;
					rigidBody3D.LinearVelocity = Vector3.Zero;
					rigidBody3D.GlobalTransform = block.GlobalTransform;
					rigidBody3D.Freeze = freeze;
				}
			}
		}
	}
}
