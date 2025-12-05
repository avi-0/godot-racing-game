using Godot;

namespace racingGame;

public partial class CarCommon : Node3D
{
	[Export] public OrbitCamera OrbitCamera;
	[Export] public AudioStreamPlayer3D EngineSoundPlayer;
}