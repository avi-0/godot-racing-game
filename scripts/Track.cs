using System.Collections.Generic;
using Godot;

namespace racingGame;

public partial class Track : Node3D
{
	public TrackOptions Options = new();
	
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
	}
}