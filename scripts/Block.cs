using System.Linq;
using Godot;

namespace racingGame;

[GlobalClass]
public partial class Block : Node3D
{
	[Export] public bool IsFinish = false;
	
	[Signal]
	public delegate void ChildMouseEnteredEventHandler(Block block);

	[Signal]
	public delegate void CarEnteredEventHandler(Car car);
	
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

		foreach (var area in FindChildren("*", "Area3D").Cast<Area3D>())
		{
			if (area.IsInGroup("finish_hitbox"))
			{
				area.BodyEntered += AreaOnBodyEntered;
			}
		}
	}

	private void AreaOnBodyEntered(Node3D body)
	{
		if (body is Car car)
		{
			EmitSignalCarEntered(car);
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
