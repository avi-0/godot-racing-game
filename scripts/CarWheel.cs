using Godot;
using racingGame;

public partial class CarWheel : ShapeCast3D
{
	[Export] public WheelConfig Config;
	
	[ExportCategory("Setup")]
	[Export]
	public bool IsFrontWheel;
	[Export]
	public bool IsRearWheel;
	[Export] 
	public Node3D WheelModel;
	
	public override void _Ready()
	{
	}
	
	public override void _Process(double delta)
	{
	}
}
