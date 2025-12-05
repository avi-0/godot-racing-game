using System.Linq;
using Godot;
using racingGame.data;

namespace racingGame;

[GlobalClass]
public partial class Block : Node3D
{
	[Signal]
	public delegate void CarEnteredEventHandler(Car car, int blockId);

	[Signal]
	public delegate void ChildMouseEnteredEventHandler(Block block);

	public BlockRecord Record;
	
	public int BlockId = 0;
	[Export] public bool IsCheckpoint = false;
	[Export] public bool IsFinish = false;
	[Export] public bool IsStart = false;
	[Export] public Node3D SpawnPointNode;

	public Transform3D SpawnPoint =>
		SpawnPointNode.GlobalTransform.Orthonormalized().RotatedLocal(Vector3.Up, float.Pi / 2);

	public override void _Ready()
	{
		foreach (var child in FindChildren("*", "CollisionObject3D").Cast<CollisionObject3D>())
			child.MouseEntered += OnChildMouseEntered;

		foreach (var child in FindChildren("*", "MeshInstance3D").Cast<MeshInstance3D>())
			for (var i = 0; i < child.Mesh.GetSurfaceCount(); i++)
			{
				var material = child.Mesh.SurfaceGetMaterial(i);
				if (material is BaseMaterial3D mat) mat.CullMode = BaseMaterial3D.CullModeEnum.Back;
			}

		foreach (var area in FindChildren("*", "Area3D").Cast<Area3D>())
			if (area.IsInGroup("finish_hitbox") || area.IsInGroup("checkpoint_hitbox"))
				area.BodyEntered += AreaOnBodyEntered;
	}

	private void AreaOnBodyEntered(Node3D body)
	{
		if (body is Car car) EmitSignalCarEntered(car, BlockId);
	}

	private void OnChildMouseEntered()
	{
		EmitSignalChildMouseEntered(this);
	}

	public void SetMaterialOverlay(Material material)
	{
		foreach (var child in FindChildren("*", "MeshInstance3D").Cast<MeshInstance3D>())
			child.MaterialOverlay = material;
	}

	public BlockPlacementData Save()
	{
		var data = new BlockPlacementData();
		data.Transform = Transform.Rounded();
		data.BlockRecordPath = ResourceUid.PathToUid(Record.ResourcePath);

		return data;
	}

	public static Block Load(BlockPlacementData data)
	{
		var record = ResourceLoader.Load<BlockRecord>(data.BlockRecordPath);
		var instance = record.Instantiate();

		instance.Transform = data.Transform;

		return instance;
	}
}
