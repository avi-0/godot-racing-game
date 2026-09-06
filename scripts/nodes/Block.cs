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

	public AudioStreamPlayer3D SpecialSoundPlayer;
	
	public int BlockId = 0;
	[Export] public bool IsCheckpoint = false;
	[Export] public bool IsFinish = false;
	[Export] public bool IsStart = false;
	[Export] public bool IsBooster = false;
	[Export] public bool IsBumper = false;
	[Export] public bool IsGrass = false;
	[Export] public bool IsPhysical = false;
	[Export] public bool HasLight = false;
	[Export] public Node3D SpawnPointNode;
	[Export] public MeshInstance3D WheelTriggerMeshInstance;
	[Export] public AudioStream SpecialSoundAudioStream = null; 
	
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

		if (IsPhysical)
		{
			if (GetChild(0) is RigidBody3D rigidBody)
			{
				rigidBody.ContactMonitor = true;
				rigidBody.MaxContactsReported = 5;
				rigidBody.BodyEntered += PhysicalHit;
			}
			
			if (SpecialSoundAudioStream == null)
			{
				SpecialSoundAudioStream = GD.Load<AudioStream>("res://audio/streams/object_drop_random_stream.tres");
			}
		}
		
		if (SpecialSoundAudioStream != null)
		{
			SpecialSoundPlayer = new AudioStreamPlayer3D();
			SpecialSoundPlayer.Stream = SpecialSoundAudioStream;
			SpecialSoundPlayer.VolumeDb = 0;
			SpecialSoundPlayer.UnitSize = 7;
			SpecialSoundPlayer.MaxDistance = 50;
			SpecialSoundPlayer.SetBus("GameSounds");
			SpecialSoundPlayer.MaxPolyphony = 4;
			GetChild(0).AddChild(SpecialSoundPlayer);
		}
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
		if (record == null) return null;
		var instance = record.Instantiate();

		instance.Transform = data.Transform;

		return instance;
	}

	private void PhysicalHit(Node body)
	{
		SpecialSoundPlayer.Play();
	}
}
