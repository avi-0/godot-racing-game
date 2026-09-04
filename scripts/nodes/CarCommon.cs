using Godot;

namespace racingGame;

public partial class CarCommon : Node3D
{
	[Export] public OrbitCamera OrbitCamera;
	[Export] public AudioStreamPlayer3D EngineSoundPlayer;
	[Export] public AudioStreamPlayer3D CrashSoundPlayer;
	[Export] public AudioStreamPlayer3D GrindSoundPlayer;
	[Export] public GpuParticles3D[] CollisionDebrisParticles;
	[Export] public Label PlayerName;
	[Export] public Sprite3D InfoSprite;
	[Export] public StandardMaterial3D RespawnMaterial;
}
