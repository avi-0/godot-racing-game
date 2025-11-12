using Godot;
using System;

namespace racingGame;

public partial class FreeCamera : Node3D
{
	[Export]
	public Camera3D Camera;
	
	public float Speed = 24.0f;
	
	private bool _active = false;

	public bool Active
	{
		get => _active;
		set
		{
			_active = value;
		}
	}

	public override void _Process(double delta)
	{
		if (!Active)
			return;
		
		var deltaF = (float)delta;
		if (Input.IsActionPressed("editor_left"))
			GlobalPosition += deltaF * Speed * GlobalBasis.X;
		if (Input.IsActionPressed("editor_right"))
			GlobalPosition -= deltaF * Speed * GlobalBasis.X;
		if (Input.IsActionPressed("editor_forward"))
			GlobalPosition += deltaF * Speed * GlobalBasis.Z;
		if (Input.IsActionPressed("editor_back"))
			GlobalPosition -= deltaF * Speed * GlobalBasis.Z;
		if (Input.IsActionPressed("editor_up"))
			GlobalPosition += deltaF * Speed * GlobalBasis.Y;
		if (Input.IsActionPressed("editor_down"))
			GlobalPosition -= deltaF * Speed * GlobalBasis.Y;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!Active)
			return;

		if (@event is InputEventMouseMotion mouseMotionEvent && Input.IsMouseButtonPressed(MouseButton.Left))
		{
			var sens = 90f / DisplayServer.ScreenGetSize().Y;

			GlobalRotationDegrees +=
				new Vector3(0, -sens * mouseMotionEvent.Relative.X, 0);

			Camera.GlobalRotationDegrees +=
				new Vector3(-sens * mouseMotionEvent.Relative.Y, 0, 0);
		}
	}

	public CameraPositionData Save()
	{
		return new CameraPositionData
		{
			Angles = new Vector3(Camera.Rotation.X, GlobalRotation.Y, 0),
			Position = GlobalPosition,
		};
	}

	public void Load(CameraPositionData data)
	{
		Camera.Rotation = new Vector3(data.Angles.X, float.Pi, 0);
		GlobalRotation = new Vector3(0, data.Angles.Y, 0);
		GlobalPosition = data.Position;
	}
}
