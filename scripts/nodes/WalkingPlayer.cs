using Godot;
using System;
using racingGame.data;

public partial class WalkingPlayer : CharacterBody3D
{
	public const float Speed = 4.0f;
	public const float JumpVelocity = 3.5f;
	
	[Export] public Camera3D Camera;
	
	public long PlayerId = -1;
	public ulong SpawnTime = 0;

	private CarInputs _inputs;
	private bool JumpNextTick = false;
	
	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor())
		{
			velocity += GetGravity() * (float)delta;
		}

		// Handle Jump.
		if (JumpNextTick && IsOnFloor())
		{
			JumpNextTick = false;
			velocity.Y = JumpVelocity;
		}
		
		Vector2 inputDir = new Vector2();
		inputDir.X = -_inputs.Left + _inputs.Right;
		inputDir.Y = -_inputs.Forward + _inputs.Back;
		
		Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y));
		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * Speed;
			velocity.Z = direction.Z * Speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
		}

		Velocity = velocity;
		MoveAndSlide();

		if (_inputs.PadCamLeft > 0.1f)
		{
			RotateY((_inputs.PadCamLeft * 0.05f));
		}
		else if (_inputs.PadCamRight > 0.1f)
		{
			RotateY((-_inputs.PadCamRight * 0.05f));
		}
	}
	
	public void SetInputs(CarInputs inputs)
	{
		_inputs = inputs;
	}

	public void Jump()
	{
		if (IsOnFloor())
		{
			JumpNextTick = true;
		}
	}
}
