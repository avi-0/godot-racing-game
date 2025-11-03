using Godot;
using System;

namespace racingGame;

public partial class Car : RigidBody3D
{
	[Signal]
	public delegate void PauseRequestedEventHandler();
	[Signal]
	public delegate void RestartRequestedEventHandler();

	[ExportCategory("Camera")] [Export] public OrbitCamera OrbitCamera;
	
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
	[Export] public float SlippingTraction = 0.1f;
	[Export] public float SlipThreshold = 0.5f;
	[Export] public float UnslipThreshold = 0.5f;
	
	[ExportCategory("Debug")]
	[Export] public bool DebugMode = false;
	
	[ExportCategory("Curves")]
	[Export] public Curve AccelerationCurve;
	[Export] public Curve SpeedSteeringCurve;
	[Export] public Curve SpeedToPitchCurve;

	[ExportCategory("Engine")]
	[Export] public AudioStreamPlayer3D EngineSound;
	
	private float _mouseSensitivity;
	private int _wheelCount;
	private bool _isReversing = false;
	private bool _isBraking = false;

	private bool _isSlipping = false;
	
	private bool _isLocallyControlled = true;
	public bool IsLocallyControlled
	{
		get => _isLocallyControlled;
		set
		{
			OrbitCamera.Camera.Current = value;
			_isLocallyControlled = value;
		}
	}
	
	public int PlayerId;
	public bool AcceptsInputs { get; set; } = false;
	
	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;

		EngineSound.Play();

		_wheelCount = Wheels.Length;
	}

	public override void _Process(double delta)
	{
		_mouseSensitivity = 1.0f * 0.25f * 2 * Mathf.Pi / DisplayServer.ScreenGetSize().Y;
		
		if (LinearVelocity.Length() > 1.0f)
			OrbitCamera.UpdateYaw((float) delta, LinearVelocity);
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
	}

	public override void _PhysicsProcess(double delta)
	{
		var wheelId = 0;
		foreach (var wheelRay in Wheels)
		{
			SteeringRotation(delta, wheelRay);

			// ебаный хак
			// проблема: если ShapeCast уже коллайдится в начальной позиции,
			// он репортит расстояние как будто бы он растягивается на полную дистанцию
			// => сначала чекнем нулевой вектор и только потом дадим какой надо
			wheelRay.TargetPosition = new Vector3();
			wheelRay.ForceShapecastUpdate();
			if (!wheelRay.IsColliding())
			{
				wheelRay.TargetPosition = new Vector3(wheelRay.TargetPosition.X, -(wheelRay.SpringRest + wheelRay.OverExtend), wheelRay.TargetPosition.Z);
				wheelRay.ForceShapecastUpdate();
			}

			ProcessSuspension(wheelRay);
			ProcessAcceleration(wheelRay);
			ProcessTraction(wheelRay, wheelId);

			wheelId++;
		}

		ProcessEngineSound();

		GameManager.Singleton.SpeedLabel.Text = ((int)Mathf.Round(LinearVelocity.Length() * 10)).ToString();

		if (DebugMode)
		{
			DebugDraw3D.DrawArrowRay(GlobalPosition, LinearVelocity, 0.5f, Color.Color8(255, 255, 255), arrow_size: 0.1f);
		}
}

	private void ProcessEngineSound()
	{
		var engineSoundTarget = 0.5f;
		if (Input.IsActionPressed("throttle") || Input.IsActionPressed("brake"))
			engineSoundTarget = 1.0f;
		
		EngineSound.VolumeDb = Mathf.LinearToDb(
			Mathf.MoveToward(Mathf.DbToLinear(EngineSound.VolumeDb), engineSoundTarget, 2 * (float)GetPhysicsProcessDeltaTime())
		);
		
		var speediness = GetSpeediness();
		EngineSound.PitchScale = SpeedToPitchCurve.Sample(Mathf.Abs(speediness));
	}

	private void ProcessSuspension(CarWheel wheelRay)
	{
		var springLength = wheelRay.TargetPosition.Length() * wheelRay.GetClosestCollisionSafeFraction();
		Vector3 wheelPos = (Vector3)wheelRay.WheelModel.Get("position");
		wheelPos.Y = Mathf.MoveToward(wheelPos.Y, -springLength, 5 * (float)GetPhysicsProcessDeltaTime());
		wheelRay.WheelModel.Set("position", wheelPos);
		
		if (wheelRay.IsColliding())
		{
			var contactPoint = wheelRay.GetCollisionPoint(0);
			var springUpDirection = wheelRay.GlobalTransform.Basis.Y;
			var offset = Mathf.Max(0, wheelRay.SpringRest - springLength);
			
			var force = wheelRay.SpringStrength * offset;
			var worldVelocity = GetPointVelocity(contactPoint);
			var relativeVelocity = springUpDirection.Dot(worldVelocity);
			var dampForce = wheelRay.SpringDamping * relativeVelocity;
			var forceVector = (force - dampForce) * springUpDirection;

			var forcePositionOffset = wheelRay.GetCollisionPoint(0) - GlobalPosition;
			ApplyForce(forceVector, forcePositionOffset);

			if (DebugMode)
			{
				//DebugDraw3D.DrawArrowRay(contactPoint, forceVector/Mass, 0.5f);
				DebugDraw3D.DrawSphere(contactPoint, wheelRay.WheelRadius * 0.1f);
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
			
			var accelerationFromCurve = AccelerationCurve.SampleBaked(Mathf.Clamp(velocity / MaxSpeed, 0, 1));
			if (velocity < 0)
				accelerationFromCurve = 1.0f;
			
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
						_isBraking = true;
					}
					else
					{
						forceVectorBackward *= ReverseSpeedMultiplier;
						_isReversing = true;
					}
				}
				else
				{
					_isBraking = false;
					_isReversing = false;
				}

				if (wheelRay.IsDriveWheel || _isReversing)
				{
					ApplyForce(forceVectorForward, forcePosition);
					ApplyForce(forceVectorBackward, forcePosition);
					if (DebugMode)
					{
						DebugDraw3D.DrawArrowRay(contactPoint, forceVectorForward / Mass, 0.5f, Color.Color8(0, 255, 0), arrow_size: 0.1f);
						DebugDraw3D.DrawArrowRay(contactPoint, forceVectorBackward / Mass, 0.5f, Color.Color8(255, 000, 0), arrow_size: 0.1f);
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
					
					targetSteering *= SpeedSteeringCurve.SampleBaked(
						Mathf.Clamp(
							Mathf.Abs(wheelRay.GlobalBasis.Z.Dot(LinearVelocity) / MaxSpeed),
							0, 1));
			}
			
			if (targetSteering != 0)
			{
				var y = Mathf.MoveToward(wheelRay.Rotation.Y, targetSteering * float.DegreesToRadians(SteeringMaxDegrees), TireTurnSpeed * delta);
				wheelRay.Rotation = new Vector3(wheelRay.Rotation.X, (float)y, wheelRay.Rotation.Z);
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
			if (tireVelocity.IsZeroApprox())
				grip = 1;
			var xTraction = wheelRay.GripCurve.SampleBaked(grip);

			SkidMarks[wheelId].GlobalPosition = wheelRay.GetCollisionPoint(0) + Vector3.Up * 0.01f;
			SkidMarks[wheelId].LookAt(wheelRay.GlobalPosition + LinearVelocity);

			var handbrake = _isBraking && !_isReversing;

			if (handbrake || grip > SlipThreshold)
			{
				_isSlipping = true;
			}
			else if (!handbrake && grip < UnslipThreshold)
			{
				_isSlipping = false;
			}

			SkidMarks[wheelId].Emitting = false;
			if (_isSlipping)
			{
				xTraction = SlippingTraction;
				SkidMarks[wheelId].Emitting = true;
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
				DebugDraw3D.DrawArrowRay(wheelRay.GlobalPosition, xForce / Mass, 0.1f, Color.Color8(0, 0, 255), arrow_size: 0.1f);
				DebugDraw3D.DrawArrowRay(wheelRay.GlobalPosition, zForce / Mass, 0.1f, Color.Color8(0, 0, 255), arrow_size: 0.1f);
			}
		}
		else
		{
			SkidMarks[wheelId].Emitting = false;
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
	
	public void Started()
	{
		OrbitCamera.SnapYaw();
	}

	private float GetSpeediness()
	{
		var velocity = Basis.Z.Dot(LinearVelocity);
		return Mathf.Clamp(velocity / MaxSpeed, -1, 1);
	}
}
