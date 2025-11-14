using Godot;
using System;

namespace racingGame;

public partial class Car : RigidBody3D
{
	[Signal]
	public delegate void PauseRequestedEventHandler();
	[Signal]
	public delegate void RestartRequestedEventHandler();

	[ExportCategory("Cameras")] 
	[Export] public OrbitCamera OrbitCamera;
	[Export] public Camera3D FrontCamera;
	
	[ExportCategory("Light")]
	[Export] public SpotLight3D HeadLight;
	
	[ExportCategory("Node Arrays")]
	[Export] public CarWheel[] Wheels;
	[Export] public GpuParticles3D[] SkidMarks;
	
	[ExportCategory("Acceleration & Braking")]
	[Export] public int Acceleration = 500;
	[Export] public int MaxSpeed = 100;
	[Export] public float BrakingStrengthMultiplier = 0.5f;
	[Export] public float ReversingStrengthMultiplier = 0.5f;

	[ExportCategory("Steering and Drifting")]
	[Export] public float TireTurnSpeed = 2.0f;
	[Export(PropertyHint.None, "degrees")] public int SteeringBaseDegrees = 25;
	[Export] public float SlippingTraction = 0.1f;
	[Export] public float SlipThreshold = 0.5f;
	[Export] public float UnslipThreshold = 0.5f;
	[Export] public float WheelZFriction = 0.05f;
	
	[ExportCategory("Debug")]
	[Export] public bool DebugMode = false;
	
	[ExportCategory("Curves")]
	[Export] public Curve AccelerationCurve;
	[Export] public Curve SpeedSteeringCurve;
	[Export] public Curve SpeedToPitchCurve;

	[ExportCategory("Engine")]
	[Export] public AudioStreamPlayer3D EngineSound;

	[ExportCategory("Wheel Setup")] 
	[Export] public WheelConfig FrontWheelConfig;
	[Export] public WheelConfig RearWheelConfig;

	[ExportCategory("Extras")] 
	[Export] private MeshInstance3D PlayerName3D;
	
	private float _mouseSensitivity;
	private int _wheelCount;
	private int _driveWheelCount;
	private bool _isAccelerating = false;
	private bool _isReversing = false;
	private bool _isBraking = false;
	private bool _hasCompressedWheel = false;

	private bool _isSlipping = false;

	private float _targetSteering;
	
	private bool _isLocallyControlled = true;
	public bool IsLocallyControlled
	{
		get => _isLocallyControlled;
		set
		{
			OrbitCamera.Camera.Current = value;

			Input.MouseMode = value ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;
			
			_isLocallyControlled = value;
		}
	}
	
	public int PlayerId;
	public bool AcceptsInputs { get; set; } = false;
	
	public override void _Ready()
	{
		EngineSound.Play();

		OrbitCamera.Radius = 3.5f;
		OrbitCamera.Pitch = float.DegreesToRadians(30);

		_wheelCount = Wheels.Length;

		SetupWheels();

		_driveWheelCount = 0;
		foreach (var wheel in Wheels)
		{
			if (wheel.Config.IsDriveWheel)
				_driveWheelCount++;
		}
	}

	private void SetupWheels()
	{
		if (FrontWheelConfig != null)
		{
			if (RearWheelConfig == null)
				RearWheelConfig = FrontWheelConfig;

			if (RearWheelConfig.SpringStrength < 0)
				RearWheelConfig.SpringStrength = FrontWheelConfig.SpringStrength;
			if (RearWheelConfig.SpringDamping < 0)
				RearWheelConfig.SpringDamping = FrontWheelConfig.SpringDamping;
			if (RearWheelConfig.SpringRest < 0)
				RearWheelConfig.SpringRest = FrontWheelConfig.SpringRest;
			if (RearWheelConfig.OverExtend < 0)
				RearWheelConfig.OverExtend = FrontWheelConfig.OverExtend;
			if (RearWheelConfig.WheelRadius < 0)
				RearWheelConfig.WheelRadius = RearWheelConfig.WheelRadius;
			if (RearWheelConfig.BaseGrip < 0)
				RearWheelConfig.BaseGrip = FrontWheelConfig.BaseGrip;
			if (RearWheelConfig.GripCurve == null)
				RearWheelConfig.GripCurve = FrontWheelConfig.GripCurve;

			foreach (var wheel in Wheels)
			{
				if (wheel.IsFrontWheel)
				{
					wheel.Config = FrontWheelConfig;
				} else if (wheel.IsRearWheel)
				{
					wheel.Config = RearWheelConfig;
				}
			}
		}
	}

	public override void _Process(double delta)
	{
		_mouseSensitivity = 1.0f * 0.25f * 2 * Mathf.Pi / DisplayServer.ScreenGetSize().Y;
		
		if (LinearVelocity.Slide(Vector3.Up).Length() > 2.0f)
			OrbitCamera.UpdateYawFromVelocity((float) delta, LinearVelocity);
	}
	
	public override void _UnhandledInput(InputEvent @event)
	{
		if (!IsLocallyControlled)
			return;
		
		if (@event.IsActionPressed("ui_cancel"))
			EmitSignalPauseRequested();
		else if (@event.IsActionPressed(InputActionNames.Restart))
			EmitSignalRestartRequested();
		else if (@event.IsActionPressed(InputActionNames.CycleCamera))
		{
			if (OrbitCamera.Camera.Current)
			{
				OrbitCamera.Camera.Current = false;
				FrontCamera.Current = true;
			}
			else
			{
				FrontCamera.Current = false;
				OrbitCamera.Camera.Current = true;
			}
			GetViewport().SetInputAsHandled();
		}
		else if(@event.IsActionPressed(InputActionNames.ToggleLights))
		{
			HeadLight.Visible = !HeadLight.Visible;
			GetViewport().SetInputAsHandled();
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		_isAccelerating = false;
		_isReversing = false;
		var velocity = GlobalBasis.Z.Dot(LinearVelocity);
		if (velocity >= 0)
		{
			_isAccelerating = Input.GetActionStrength("game_forward") > 0;
			_isBraking = Input.GetActionStrength("game_back") > 0;
		}
		else
		{
			_isReversing = Input.GetActionStrength("game_back") > 0;
			_isBraking = Input.GetActionStrength("game_forward") > 0;
		}

		_hasCompressedWheel = false;
		foreach (var wheel in Wheels)
		{
			SteeringRotation(delta, wheel);

			// ебаный хак
			// проблема: если ShapeCast уже коллайдится в начальной позиции,
			// он репортит расстояние как будто бы он растягивается на полную дистанцию
			// => сначала чекнем нулевой вектор и только потом дадим какой надо
			wheel.TargetPosition = new Vector3();
			wheel.ForceShapecastUpdate();
			if (!wheel.IsColliding())
			{
				wheel.TargetPosition = new Vector3(wheel.TargetPosition.X, -(wheel.Config.SpringRest + wheel.Config.OverExtend), wheel.TargetPosition.Z);
				wheel.ForceShapecastUpdate();
			}

			ProcessSuspension(wheel);
		}

		// ускорение и повороты - только если есть хотя бы одно колесо,
		// которое прижато к земле (т.е. подвеска сжата, а не растянута)
		// чтобы когда тачка уже в воздухе, она не поворачивала от лёгкого задева колесом
		if (_hasCompressedWheel)
		{
			var wheelId = 0;
			foreach (var wheel in Wheels)
			{
				ProcessAcceleration(wheel);
				ProcessTraction(wheel, wheelId);

				wheelId++;
			}
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
		if (Input.IsActionPressed(InputActionNames.Forward) || Input.IsActionPressed(InputActionNames.Back))
			engineSoundTarget = 1.0f;
		
		EngineSound.VolumeDb = Mathf.LinearToDb(
			Mathf.MoveToward(Mathf.DbToLinear(EngineSound.VolumeDb), engineSoundTarget, 2 * (float)GetPhysicsProcessDeltaTime())
		);
		
		var speediness = GetSpeediness();
		EngineSound.PitchScale = SpeedToPitchCurve.Sample(Mathf.Abs(speediness));
	}

	private void ProcessSuspension(CarWheel wheel)
	{
		var springLength = wheel.TargetPosition.Length() * wheel.GetClosestCollisionSafeFraction();
		Vector3 wheelPos = wheel.WheelModel.Position;
		wheelPos.Y = Mathf.MoveToward(wheelPos.Y, -springLength, 5 * (float)GetPhysicsProcessDeltaTime());
		wheel.WheelModel.Position = wheelPos;
		
		if (wheel.IsColliding())
		{
			for (int i = 0; i < wheel.GetCollisionCount(); i++)
			{
				var contactPoint = wheel.GetCollisionPoint(i);
				var normal = wheel.GetCollisionNormal(i);
				
				// doesn't work well for spherical tires
				//if (normal.Dot(wheelRay.GlobalBasis.Y) < 0.95)
				//	continue;
				
				var springUpDirection = wheel.GlobalTransform.Basis.Y;
				var offset = Mathf.Max(0, wheel.Config.SpringRest - springLength);
				if (offset > 0)
					_hasCompressedWheel = true;
			
				var force = wheel.Config.SpringStrength * offset;
				var worldVelocity = GetPointVelocity(contactPoint);
				var relativeVelocity = springUpDirection.Dot(worldVelocity);
				var dampForce = wheel.Config.SpringDamping * relativeVelocity;
				var susForce = (force - dampForce);
				var forceVector = susForce * springUpDirection;

				var forcePositionOffset = wheel.GetCollisionPoint(0) - GlobalPosition;

				if (Math.Abs(GetLinearVelocity().Length()) < 5.0) // чтобы с наклонных поверхностей не скатывало
				{
					var susP = GlobalBasis.Y * susForce;
					forceVector.Z -= susP.Z * GlobalBasis.Y.Dot(Vector3.Up);
					forceVector.X -= susP.X * GlobalBasis.Y.Dot(Vector3.Up);
				}
				
				ApplyForce(forceVector, forcePositionOffset);

				if (DebugMode)
				{
					//DebugDraw3D.DrawArrowRay(contactPoint, forceVector/Mass, 0.5f);
					DebugDraw3D.DrawSphere(contactPoint, wheel.Config.WheelRadius * 0.1f);
				}
			}
		}
	}

	void ProcessAcceleration(CarWheel wheel)
	{
		var forwardDir = wheel.GlobalBasis.Z;
		var carForwardDir = GlobalBasis.Z;
		var velocity = carForwardDir.Dot(LinearVelocity);
		wheel.WheelModel.RotateX((-velocity * (float)GetProcessDeltaTime()) / wheel.Config.WheelRadius);
		
		var forwardStrength = Input.GetActionStrength(InputActionNames.Forward);
		var backStrength = -Input.GetActionStrength(InputActionNames.Back);
		if (!AcceptsInputs)
		{
			forwardStrength = 0;
			backStrength = 0;
		}
		
		var accelerationFromCurve = AccelerationCurve.SampleBaked(Mathf.Clamp(velocity / MaxSpeed, 0, 1));
		if (velocity < 0)
			accelerationFromCurve = 1.0f;

		float accelerationStrength = 0;
		if (velocity >= 0)
			accelerationStrength = forwardStrength;
		else
			accelerationStrength = backStrength * ReversingStrengthMultiplier;

		float brakeStrength = 0;
		if (_isBraking)
		{
			if (velocity >= 0)
				brakeStrength = backStrength;
			else
				brakeStrength = forwardStrength;
		}
		
		if (!AcceptsInputs)
			brakeStrength = -float.Sign(velocity);
		
		var contactPoint = wheel.WheelModel.GlobalPosition;
		var accelerationForce = forwardDir * Acceleration * accelerationStrength * accelerationFromCurve;
		var brakingForce = carForwardDir * Acceleration * brakeStrength * BrakingStrengthMultiplier * accelerationFromCurve;
		var forcePosition = contactPoint - GlobalPosition;
		
		if (wheel.IsColliding())
		{
			if (wheel.Config.IsDriveWheel)
			{
				ApplyForce(accelerationForce / _driveWheelCount, forcePosition);
			}
			ApplyForce(brakingForce / _wheelCount, forcePosition);
			if (DebugMode)
			{
				DebugDraw3D.DrawArrowRay(contactPoint, accelerationForce / Mass, 0.5f, Color.Color8(0, 255, 0), arrow_size: 0.1f);
				DebugDraw3D.DrawArrowRay(contactPoint, brakingForce / Mass, 0.5f, Color.Color8(255, 000, 0), arrow_size: 0.1f);
			}
		}
	}
	
	void SteeringRotation(double delta, CarWheel wheel)
	{
		if (wheel.Config.IsSteeringWheel)
		{
			_targetSteering = 0;
			if (AcceptsInputs)
			{
					_targetSteering += Input.GetActionStrength(InputActionNames.Left);
					_targetSteering -= Input.GetActionStrength(InputActionNames.Right);
					
					_targetSteering *= SpeedSteeringCurve.SampleBaked(
						Mathf.Clamp(
							Mathf.Abs(wheel.GlobalBasis.Z.Dot(LinearVelocity) / MaxSpeed),
							0, 1));
			}
			
			if (_targetSteering != 0)
			{
				var y = Mathf.MoveToward(wheel.Rotation.Y, _targetSteering * float.DegreesToRadians(SteeringBaseDegrees), TireTurnSpeed * delta);
				wheel.Rotation = new Vector3(wheel.Rotation.X, (float)y, wheel.Rotation.Z);
			}
			else
			{
				var y = Mathf.MoveToward(wheel.Rotation.Y, 0, TireTurnSpeed * delta);
				wheel.Rotation = new Vector3(wheel.Rotation.X, (float)y, wheel.Rotation.Z);
			}
		}
	}

	void ProcessTraction(CarWheel wheel, int wheelId)
	{
		var tireWeight = (Mass * -GetGravity().Y) / _wheelCount;
		
		if (wheel.IsColliding())
		{
			for (int i = 0; i < wheel.GetCollisionCount(); i++)
			{
				var contactPoint = wheel.GetCollisionPoint(i);
				var normal = wheel.GetCollisionNormal(i);

				if (normal.Dot(wheel.GlobalBasis.Y) < 0.95)
					continue;
				
				var steerSideDirection = wheel.GlobalBasis.Z.Cross(normal).Normalized();
				var tireVelocity = GetPointVelocity(contactPoint);
				var steerXVelocity = steerSideDirection.Dot(tireVelocity);

				var grip = Mathf.Abs(steerXVelocity / tireVelocity.Length());
				if (tireVelocity.IsZeroApprox())
					grip = 1;

				var curveValue = wheel.Config.GripCurve?.SampleBaked(grip) ?? 1.0f;
				var xTraction = curveValue * wheel.Config.BaseGrip;

				SkidMarks[wheelId].GlobalPosition = wheel.GetCollisionPoint(0) + Vector3.Up * 0.01f;
				SkidMarks[wheelId].LookAt(wheel.GlobalPosition + LinearVelocity);

				var handbrake = _isBraking && _isAccelerating;

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
					
					if (wheel.Config.FullLoseGripOnSlip && tireVelocity.Length() > 2 && _targetSteering != 0)
					{
						xTraction = 0;
					}
				}
			
				var xForce = -steerSideDirection * steerXVelocity * xTraction * tireWeight;

				var fVelocity = -wheel.GlobalBasis.Z.Dot(tireVelocity);
				var zTraction = WheelZFriction;
				var zForce = wheel.GlobalBasis.Z * fVelocity * zTraction * tireWeight;
			
				var forcePos = contactPoint - GlobalPosition;
				ApplyForce(xForce / wheel.GetCollisionCount(), forcePos);
				ApplyForce(zForce / wheel.GetCollisionCount(), forcePos);
				if (DebugMode)
				{
					DebugDraw3D.DrawArrowRay(contactPoint, xForce / Mass, 0.1f, Color.Color8(0, 0, 255), arrow_size: 0.1f);
					DebugDraw3D.DrawArrowRay(contactPoint, zForce / Mass, 0.1f, Color.Color8(0, 0, 255), arrow_size: 0.1f);
				}
			}
		}
		else
		{
			SkidMarks[wheelId].Emitting = false;
		}
	}

	private Vector3 GetPointVelocity(Vector3 point)
	{
		return LinearVelocity + AngularVelocity.Cross(point - ToGlobal(CenterOfMass));
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

	public void SetPlayerName(string name)
	{
		name = name.Trim().Normalize();
		if (name.Length > 10) {name = name.Substring(0, 10);}

		void setFontSize(int size)
		{
			PlayerName3D.Mesh.Set("font_size", size);
		}
		
		setFontSize(10);
		switch (name.Length)
		{
			case 4:
				setFontSize(8);
				break;
			case 5:
				setFontSize(6);
				break;
			case 6:
				setFontSize(5);
				break;
			case 7:
			case 8:
			case 9:
			case 10:
			case 11:
			case 12:
				setFontSize(4);
				break;
		}
		
		PlayerName3D.Mesh.Set("text", name);
	}
}
