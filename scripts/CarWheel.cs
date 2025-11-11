using Godot;
using racingGame;

public partial class CarWheel : ShapeCast3D
{
	[Export] public WheelConfig Config;
	
	[ExportCategory("Setup")]
	[Export] public bool IsFrontWheel;
	[Export] public bool IsRearWheel;

	public Node3D WheelModel;
	
	public override void _Ready()
	{
		WheelModel = (Node3D)GetChild(0);
	}
	
	public override void _Process(double delta)
	{
	}
}
