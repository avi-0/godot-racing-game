using Godot;
using System;
using racingGame;

public partial class FollowEffect : Node3D
{
	[Export] public MeshInstance3D Water;
	[Export] public GpuParticles3D Weather;
	[Export] public GpuParticlesCollisionHeightField3D CollisionHeightField;
	
	public override void _Ready()
	{}
	
	public override void _Process(double delta)
	{}
}
