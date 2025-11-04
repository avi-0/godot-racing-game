using Godot;
using System;

public partial class CarWheel : ShapeCast3D
{
	[ExportCategory("Suspension")]
	[Export] public int SpringStrength = 100;
	[Export] public float SpringDamping = 2.0f;
	[Export] public float SpringRest = 0.5f;
	[Export] public float OverExtend = 0.2f;
	
	[ExportCategory("Wheel Parameters")]
	[Export] public float WheelRadius = 0.5f;
	[Export] public bool IsDriveWheel = false;
	[Export] public bool IsSteerWheel = false;

	[ExportCategory("Curves")]
	[Export] public Curve GripCurve;

	public Node3D WheelModel;

	public bool IsSlipping = false;
	
	public override void _Ready()
	{
		WheelModel = (Node3D)GetChild(0);
	}
	
	public override void _Process(double delta)
	{
	}
}
