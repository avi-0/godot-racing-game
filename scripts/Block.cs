using System.Linq;
using Godot;

namespace racingGame.blocks;

[GlobalClass]
public partial class Block : Node3D
{
	[Signal]
	public delegate void ChildMouseEnteredEventHandler(Block block);
	
	public override void _Ready()
	{
		foreach (var child in FindChildren("*", "CollisionObject3D").Cast<CollisionObject3D>())
		{
			child.MouseEntered += OnChildMouseEntered;
		}
	}

	private void OnChildMouseEntered()
	{
		EmitSignalChildMouseEntered(this);
	}

	public void SetMaterialOverlay(Material material)
	{
		foreach (var child in FindChildren("*", "MeshInstance3D").Cast<MeshInstance3D>())
		{
			child.MaterialOverlay = material;
		}
	}
}
