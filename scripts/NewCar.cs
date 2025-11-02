using Godot;
using System;
using System.Diagnostics;

namespace racingGame;
public partial class NewCar : RigidBody3D
{
	[Signal]
	public delegate void PauseRequestedEventHandler();
	[Signal]
	public delegate void RestartRequestedEventHandler();
	
	[ExportCategory("Camera")]
	[Export] public Camera3D Camera;
	[Export] public Node3D CameraStick;
	[Export] public Node3D CameraStickBase;
	
	[ExportCategory("Node Arrays")]
	[Export] public CarWheel[] Wheels;
	[Export] public GpuParticles3D[] SkidMarks;
	
	[ExportCategory("Acceleration & Braking")]
	[Export] public int Acceleration = 500;
	[Export] public int MaxSpeed = 100;
	[Export] public float BrakingSpeedMultiplier = 0.3f;
	[Export] public float ReverseSpeedMultiplier = 0.5f;

	[ExportCategory("Steering and Drifting")]
	[Export] public float TireTurnSpeed = 2.0f;
	[Export] public int SteeringMaxDegrees = 25;
	[Export] public float SlippingTraction = 0.5f;
	[Export] public float BrakingTraction = 0.1f;
	
	[ExportCategory("Debug")]
	[Export] public bool DebugMode = false;
	
	[ExportCategory("Curves")]
	[Export] public Curve AccelerationCurve;
	[Export] public Curve SpeedSteeringCurve;
	
	private float _mouseSensitivity;
	private int _wheelCount;
	private bool _isReversing = false;
	private bool _isBreaking = false;
	
	private bool _isLocallyControlled = true;
	public bool IsLocallyControlled
	{
		get => _isLocallyControlled;
		set
		{
			Camera.Current = value;
			_isLocallyControlled = value;
		}
	}
	
	public int PlayerId;
	public bool AcceptsInputs { get; set; } = false;
	public bool MagnetEffect { get; set; } = false;
	
	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;

		_wheelCount = Wheels.Length;
	}

	public override void _Process(double delta)
	{
		_mouseSensitivity = 1.0f * 0.25f * 2 * Mathf.Pi / DisplayServer.ScreenGetSize().Y;
		
		ControlCamera();
		UpdateCameraYaw((float)delta);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!IsLocallyControlled)
			return;

		if (@event is InputEventKey keyEvent && keyEvent.IsPressed())
		{
			if (keyEvent.PhysicalKeycode == Key.Escape)
				EmitSignalPauseRequested();
			else if (keyEvent.PhysicalKeycode == Key.R) EmitSignalRestartRequested();
		}
		
		if (@event is InputEventMouseMotion motionEvent && Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			var delta = motionEvent.Relative;
			CameraStick.Rotation = new Vector3(
				CameraStick.Rotation.X - delta.Y * _mouseSensitivity,
				CameraStick.Rotation.Y - delta.X * _mouseSensitivity,
				CameraStick.Rotation.Z
			);

			ControlCamera();
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		var wheelId = 0;
		foreach (var wheelRay in Wheels)
		{
			SteeringRotation(delta, wheelRay);

			wheelRay.ForceRaycastUpdate();

			ProcessSuspension(wheelRay);
			ProcessAcceleration(wheelRay);
			ProcessTraction(wheelRay, wheelId);

			wheelId++;
		}

		GameManager.Singleton.SpeedLabel.Text = ((int)Mathf.Round(LinearVelocity.Length())).ToString();

		if (DebugMode)
		{
			DebugDraw3D.DrawArrowRay(GlobalPosition, LinearVelocity, 0.5f, Color.Color8(255, 255, 255));
		}
}

	private void ProcessSuspension(CarWheel wheelRay)
	{
		if (wheelRay.IsColliding())
		{
			if (!MagnetEffect)
			{
				wheelRay.TargetPosition = new Vector3(wheelRay.TargetPosition.X, -(wheelRay.SpringRest + wheelRay.WheelRadius + wheelRay.OverExtend), wheelRay.TargetPosition.Z);
			}
			
			var contactPoint = wheelRay.GetCollisionPoint();
			var springUpDirection = wheelRay.GlobalTransform.Basis.Y;
			var springLength = Mathf.Max(0.0f, wheelRay.GlobalPosition.DistanceTo(contactPoint) - wheelRay.WheelRadius);
			var offset = wheelRay.SpringRest - springLength;

			Vector3 wheelPos = (Vector3)wheelRay.WheelModel.Get("position");
			wheelPos.Y = Mathf.MoveToward(wheelPos.Y, -springLength, 5 * (float)GetPhysicsInterpolationMode());
			wheelRay.WheelModel.Set("position", wheelPos);
			
			var force = wheelRay.SpringStrength * offset;
			var worldVelocity = GetPointVelocity(contactPoint);
			var relativeVelocity = springUpDirection.Dot(worldVelocity);
			var dampForce = wheelRay.SpringDamping * relativeVelocity;
			var forceVector = (force - dampForce) * wheelRay.GetCollisionNormal();

			contactPoint = wheelRay.WheelModel.GlobalPosition;
			
			var forcePositionOffset = contactPoint - GlobalPosition;
			ApplyForce(forceVector, forcePositionOffset);

			if (DebugMode)
			{
				//DebugDraw3D.DrawArrowRay(contactPoint, forceVector/Mass, 0.5f);
				//DebugDraw3D.DrawSphere(wheelRay.WheelModel.GlobalPosition, wheelRay.WheelRadius);
			}
		}
	}

	void ProcessAcceleration(CarWheel wheelRay)
	{
		var forwardDir = wheelRay.GlobalBasis.Z;
		var velocity = forwardDir.Dot(LinearVelocity);
		wheelRay.WheelModel.RotateX((-velocity * (float)GetProcessDeltaTime())/wheelRay.WheelRadius);
		
		if (AcceptsInputs && (Input.IsActionPressed("throttle") || Input.IsActionPressed("brake")))
		{
			var throttleStrength = Input.GetActionStrength("throttle");
			var brakeStrength = -Input.GetActionStrength("brake");
			
			var accelerationFromCurve = AccelerationCurve.SampleBaked(velocity / MaxSpeed);
			var contactPoint = wheelRay.WheelModel.GlobalPosition;
			var forceVectorForward = forwardDir * Acceleration * throttleStrength * accelerationFromCurve;
			var forceVectorBackward = forwardDir * Acceleration * brakeStrength * accelerationFromCurve;
			var forcePosition = contactPoint - GlobalPosition;
			
			if (wheelRay.IsColliding())
			{
				if (brakeStrength < 0)
				{
					if (velocity > 0)
					{
						forceVectorBackward *= BrakingSpeedMultiplier;
						_isBreaking = true;
					}
					else
					{
						forceVectorBackward *= ReverseSpeedMultiplier;
						_isReversing = true;
					}
				}
				else
				{
					_isBreaking = false;
					_isReversing = false;
				}

				if (wheelRay.IsDriveWheel || _isReversing)
				{
					ApplyForce(forceVectorForward, forcePosition);
					ApplyForce(forceVectorBackward, forcePosition);
					if (DebugMode)
					{
						DebugDraw3D.DrawArrowRay(contactPoint, forceVectorForward / Mass, 0.5f, Color.Color8(0, 255, 0));
						DebugDraw3D.DrawArrowRay(contactPoint, forceVectorBackward / Mass, 0.5f, Color.Color8(255, 000, 0));
					}
				}
			}
		}
	}
	
	void SteeringRotation(double delta, CarWheel wheelRay)
	{
		if (wheelRay.IsSteerWheel)
		{
			float targetSteering = 0;
			if (AcceptsInputs)
			{
					targetSteering += Input.GetActionStrength("steer_left");
					targetSteering -= Input.GetActionStrength("steer_right");
					
					targetSteering *= SpeedSteeringCurve.SampleBaked(Mathf.Abs(wheelRay.GlobalBasis.Z.Dot(LinearVelocity)/ MaxSpeed));
			}
			
			if (targetSteering != 0)
			{
				var y = Mathf.MoveToward(wheelRay.Rotation.Y, targetSteering, TireTurnSpeed * delta);
				wheelRay.Rotation = new Vector3(wheelRay.Rotation.X, Math.Clamp((float)y, float.DegreesToRadians(-SteeringMaxDegrees), float.DegreesToRadians(SteeringMaxDegrees)), wheelRay.Rotation.Z);
			}
			else
			{
				var y = Mathf.MoveToward(wheelRay.Rotation.Y, 0, TireTurnSpeed * delta);
				wheelRay.Rotation = new Vector3(wheelRay.Rotation.X, (float)y, wheelRay.Rotation.Z);
			}
		}
	}

	void ProcessTraction(CarWheel wheelRay, int wheelId)
	{
		if (wheelRay.IsColliding())
		{
			var tireWeight = (Mass * -GetGravity().Y) / _wheelCount;
			
			var contactPoint = wheelRay.WheelModel.GlobalPosition;
			var steerSideDirection = wheelRay.GlobalBasis.X;
			var tireVelocity = GetPointVelocity(contactPoint);
			var steerXVelocity = steerSideDirection.Dot(tireVelocity);

			var grip = Mathf.Abs(steerXVelocity / tireVelocity.Length()); 
			var xTraction = wheelRay.GripCurve.SampleBaked(grip);

			SkidMarks[wheelId].GlobalPosition = wheelRay.GetCollisionPoint() + Vector3.Up * 0.01f;
			SkidMarks[wheelId].LookAt(wheelRay.GlobalPosition + LinearVelocity);

			if (!_isBreaking)
			{
				SkidMarks[wheelId].Emitting = false;
				if (grip > 0.2)
				{
					xTraction = SlippingTraction;
					//SkidMarks[wheelId].Emitting = true;
				}
			}
			else
			{
				SkidMarks[wheelId].Emitting = true;
				xTraction = BrakingTraction;
			}
			
			var xForce = -steerSideDirection * steerXVelocity * xTraction * tireWeight;

			var fVelocity = -wheelRay.GlobalBasis.Z.Dot(tireVelocity);
			var zTraction = 0.05f;
			var zForce = wheelRay.GlobalBasis.Z * fVelocity * zTraction * tireWeight;
			
			var forcePos = wheelRay.WheelModel.GlobalPosition - GlobalPosition;
			ApplyForce(xForce, forcePos);
			ApplyForce(zForce, forcePos);
			if (DebugMode)
			{
				DebugDraw3D.DrawArrowRay(wheelRay.GlobalPosition, xForce / Mass, 0.1f, Color.Color8(0, 0, 255));
				DebugDraw3D.DrawArrowRay(wheelRay.GlobalPosition, zForce / Mass, 0.1f, Color.Color8(0, 0, 255));
			}
		}
	}

	private Vector3 GetPointVelocity(Vector3 point)
	{
		return LinearVelocity + AngularVelocity.Cross(point - GlobalPosition);
	}
	
	private float GetCameraTargetYaw(Vector3 dir)
	{
		return -dir.Slide(Vector3.Up).SignedAngleTo(Vector3.Back, Vector3.Up);
	}

	private void UpdateCameraYaw(float delta)
	{
		if (LinearVelocity.Length() > 1.0f)
		{
			var target = GetCameraTargetYaw(LinearVelocity);
			CameraStickBase.Rotation = new Vector3(
				CameraStickBase.Rotation.X,
				Mathf.LerpAngle(target, CameraStickBase.Rotation.Y, Mathf.Exp(-5.0f * delta)),
				CameraStickBase.Rotation.Z
			);
		}
	}

	private void SnapCameraYaw()
	{
		CameraStickBase.Rotation =
			new Vector3(CameraStickBase.Rotation.X, GetCameraTargetYaw(GlobalBasis.Z), CameraStickBase.Rotation.Z);
	}
	
	public void Started()
	{
		SnapCameraYaw();
	}
	
	private void ControlCamera()
	{
		if (Input.IsMouseButtonPressed(MouseButton.Left))
			CameraStick.Rotation = new Vector3(
				CameraStick.Rotation.X,
				Mathf.Pi,
				CameraStick.Rotation.Z
			);
	}
}
