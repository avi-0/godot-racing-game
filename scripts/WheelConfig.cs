using Godot;

namespace racingGame;

[GlobalClass]
public partial class WheelConfig : Resource
{
	[ExportCategory("Suspension")]
	[Export] public float SpringStrength = -1;
	[Export] public float SpringDamping = -1;
	[Export] public float SpringRest = -1;
	[Export] public float OverExtend = -1;
	
	[ExportCategory("Wheel Parameters")]
	[Export] public float WheelRadius = -1;
	[Export] public bool IsDriveWheel = false;
	[Export] public bool IsSteeringWheel = false;

	[ExportCategory("Grip")]
	[Export] public float BaseGrip = 1.0f;
	[Export] public Curve GripCurve;
}