using System.Linq;
using Godot;

namespace racingGame;

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

		foreach (var child in FindChildren("*", "MeshInstance3D").Cast<MeshInstance3D>())
		{
			for (int i = 0; i < child.Mesh.GetSurfaceCount(); i++)
			{
				var material = child.Mesh.SurfaceGetMaterial(i);
				if (material is BaseMaterial3D mat)
				{
					mat.CullMode = BaseMaterial3D.CullModeEnum.Back;
				}
			}
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
