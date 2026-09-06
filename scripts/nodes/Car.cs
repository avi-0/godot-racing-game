using Godot;
using System;
using System.Collections.Generic;
using racingGame.data;

namespace racingGame;

public partial class Car : RigidBody3D
{
	[Signal]
	public delegate void RestartRequestedEventHandler();

	[ExportCategory("Components")] 
	[Export] public Node3D CarModel;
	[Export] public Camera3D FrontCamera;
	[Export] public SpotLight3D[] HeadLights;
	[Export] public SpotLight3D[] RearLights;
	[Export] public CarCommon CarCommon;
	[Export] public MeshInstance3D Nameplate;
	[Export] public CarWheel[] Wheels;
	[Export] public MultiplayerSynchronizer MultiplayerSynchronizer;
	[Export] public Node3D EnginePosition;
	
	[ExportCategory("Acceleration & Braking")]
	[Export] public int Acceleration = 500;
	[Export] public int MaxSpeed = 100;
	[Export] public float BrakingStrengthMultiplier = 0.5f;
	[Export] public float ReversingStrengthMultiplier = 0.5f;
	[Export] public bool ReleaseDebuff = false;
	[Export] public AudioStreamWav EngineSoundStream;
	
	[ExportCategory("Steering and Drifting")]
	[Export] public float TireTurnSpeed = 2.0f;
	[Export(PropertyHint.None, "degrees")] public int SteeringBaseDegrees = 25;
	[Export] public float SlippingTraction = 0.1f;
	[Export] public float SlipThreshold = 0.5f;
	[Export] public float UnslipThreshold = 0.5f;
	[Export] public float WheelZFriction = 0.05f;
	[Export] public bool SteeringAffectsCenterOfMass = false;
	[Export] public bool FullGripOffroad = false;
	
	[ExportCategory("Debug")]
	[Export] public bool DebugMode = false;

	[ExportCategory("Downforce")] 
	[Export] public float MaxDownforce = 0f;
	
	[ExportCategory("Curves")]
	[Export] public Curve AccelerationCurve;
	[Export] public Curve SpeedSteeringCurve;
	[Export] public Curve SpeedToPitchCurve;
	[Export] public Curve SpeedToDownforceCurve;

	[ExportCategory("Wheel Setup")] 
	[Export] public WheelConfig FrontWheelConfig;
	[Export] public WheelConfig RearWheelConfig;
	
	[ExportCategory("Descriptions")]
	[Export] public string CarName = "Default";
	[Export] public string CarDescription = "No Description";

	[ExportCategory("Skins")]
	[Export] public Material[] Skins;
	
	public bool IsGhost = false;
	
	private float _mouseSensitivity;
	private int _wheelCount;
	private int _driveWheelCount;
	private bool _isAccelerating = false;
	private bool _isReversing = false;
	private bool _isBraking = false;
	private bool _hasCompressedWheel = false;
	private bool _isSlipping = false;
	private float _targetSteering;
	
	private float _defaultDamp = 0;
	private Vector3 _defaultCOM = Vector3.Zero;
	
	private bool _isLocallyControlled = false;
	public bool IsLocallyControlled
	{
		get => _isLocallyControlled;
		set
		{
			Input.MouseMode = value ? Input.MouseModeEnum.Captured : Input.MouseModeEnum.Visible;

			if (value)
			{
				CarCommon.AudioListener.MakeCurrent();
			}
			else
			{
				CarCommon.AudioListener.ClearCurrent();
			}

			_isLocallyControlled = value;
		}
	}
	
	public long PlayerId = -1;
	public bool AcceptsInputs { get; set; } = false;

	public OrbitCamera OrbitCamera => CarCommon.OrbitCamera;
	public AudioStreamPlayer3D EngineSoundPlayer => CarCommon.EngineSoundPlayer;

	private CarInputs _inputs = new();

	private Queue<float> _speedQueue = new();

	private RayCast3D _rayCastUp;

	private bool _frozen = false;
	private Vector3 _frozenPosition;

	private int _bonkCount = 0;

	private int _currentSkin = 0;

	private int _wheelsOnBump = 0;
	
	public override void _Ready()
	{
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

		ContactMonitor = true;
		MaxContactsReported = 5;
		BodyEntered += Bonk;
		BodyExited += Unbonk;

		for (int i = 0; i < 5; i++)
		{
			_speedQueue.Enqueue(0);
		}

		_defaultDamp = LinearDamp;
		_defaultCOM = CenterOfMass;

		if (!IsGhost)
		{
			_PhysicsProcess(60);
		}

		CarCommon.EngineSoundPlayer.Stream = EngineSoundStream;
		CarCommon.EngineSoundPlayer.Transform = EnginePosition.Transform;
		CarCommon.EngineSoundPlayer.Playing = true;
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
				RearWheelConfig.WheelRadius = FrontWheelConfig.WheelRadius;
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

	public void SetInputs(CarInputs inputs)
	{
		_inputs = inputs;
	}

	public void InputRestart()
	{
		EmitSignalRestartRequested();
	}

	public void InputToggleLights(int on = -1)
	{
		for (int light = 0; light < HeadLights.Length; light++)
		{
			if (on == -1)
			{
				HeadLights[light].Visible = !HeadLights[light].Visible;
			}
			else
			{
				HeadLights[light].Visible = on == 1;
			}
		}

		if (CarModel.GetChild(0) is MeshInstance3D mesh)
		{
			StandardMaterial3D material = (StandardMaterial3D)mesh.GetActiveMaterial(0);
			material.EmissionEnabled = HeadLights[0].Visible;
			CarModel.TreeExited += () => { material.EmissionEnabled = false; };
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		//if ( MultiplayerManager.Instance.OnServer && !IsMultiplayerAuthority()) { return;}

		if (_frozen)
		{
			GlobalPosition = _frozenPosition;
			LinearVelocity = new Vector3(0, 0, 0);
			AngularVelocity = new Vector3(0, 0, 0);
			return;
		}
		
		_isAccelerating = false;
		_isReversing = false;
		var velocity = GlobalBasis.Z.Dot(LinearVelocity);
		if (velocity >= 0)
		{
			_isAccelerating = _inputs.Forward > 0;
			_isBraking = _inputs.Back > 0;
		}
		else
		{
			_isReversing = _inputs.Back > 0;
			_isBraking = _inputs.Forward > 0;
		}

		_hasCompressedWheel = false;
		_wheelsOnBump = 0;
		foreach (var wheel in Wheels)
		{
			SteeringRotation(delta, wheel);

			// ебаный хак
			// проблема: если ShapeCast уже коллайдится в начальной позиции,
			// он репортит расстояние как будто бы он растягивается на полную дистанцию
			// => сначала чекнем нулевой вектор и только потом дадим какой надо
			wheel.ShapeCast.TargetPosition = new Vector3();
			wheel.ShapeCast.ForceShapecastUpdate();
			if (!wheel.ShapeCast.IsColliding())
			{
				wheel.ShapeCast.TargetPosition = new Vector3(-(wheel.Config.SpringRest + wheel.Config.OverExtend), 0, 0);
				wheel.ShapeCast.ForceShapecastUpdate();
			}

			ProcessSuspension(wheel);
			
			wheel.GrassContact = false;
			ProcessSpecialBlocks(wheel);
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
			
			ProcessDownForce();
		}
		
		ProcessEngineSound();

		if (LinearVelocity.Slide(Vector3.Up).Length() > 2.0f)
			OrbitCamera.UpdateYawFromVelocity((float) delta, LinearVelocity);
		
		if (DebugMode)
		{
			DebugDraw3D.DrawArrowRay(GlobalPosition, LinearVelocity, 0.5f, Color.Color8(255, 255, 255), arrow_size: 0.1f);
		}
		
		_speedQueue.Dequeue(); _speedQueue.Enqueue(LinearVelocity.Length()); // for bonk strength calculation
		if (LinearVelocity.Length() < 2)
		{
			CarCommon.GrindSoundPlayer.Stop();
		}
		
		//water fall death
		if (GetGlobalPosition().Y < GameManager.DeathY)
		{
			_frozen = true;
			_frozenPosition = GlobalPosition;
			CarModel.Hide();
			if (PlayerId >= 0 && GameModeController.CurrentGameMode.GetPlayer(PlayerId).State == GameModeUtils.PLAYER_STATE_PLAYING)
			{
				GameModeController.CurrentGameMode.GetPlayer(PlayerId).State = GameModeUtils.PLAYER_STATE_DEAD;
			}
		}
		//--
	}

	private void ProcessEngineSound()
	{
		var engineSoundTarget = 1.0f;
		
		EngineSoundPlayer.VolumeDb = Mathf.LinearToDb(
			Mathf.MoveToward(Mathf.DbToLinear(EngineSoundPlayer.VolumeDb), engineSoundTarget, 2 * (float)GetPhysicsProcessDeltaTime())
		);
		
		var speediness = GetSpeediness();
		EngineSoundPlayer.PitchScale = SpeedToPitchCurve.Sample(Mathf.Abs(speediness));
		if (!((!_isReversing && _inputs.Forward > 0 ) || (_isReversing && _inputs.Back > 0)))
		{
			EngineSoundPlayer.PitchScale *= 0.65f;
		}
	}

	private void ProcessSuspension(CarWheel wheel)
	{
		var springLength = wheel.ShapeCast.TargetPosition.Length() * wheel.ShapeCast.GetClosestCollisionSafeFraction();
		Vector3 wheelPos = wheel.WheelModel.Position;
		wheelPos.Y = Mathf.MoveToward(wheelPos.Y, -springLength, 5 * (float)GetPhysicsProcessDeltaTime());
		wheel.WheelModel.Position = wheelPos;

		// suspension sound
		if (wheel.SpringLengths.Count < 15)
		{
			wheel.SpringLengths.Enqueue(springLength);
		}
		else
		{
			float avgLength = 0;
			for (int i = 0; i < wheel.SpringLengths.Count; i++)
			{
				float peek = wheel.SpringLengths.Dequeue();
				avgLength += peek;
				wheel.SpringLengths.Enqueue(peek);
			}
			avgLength /= wheel.SpringLengths.Count;
			float lengthChange = Math.Abs(avgLength - springLength);

			//GD.Print((wheel.Config.SpringStrength-wheel.Config.SpringDamping)/150000 + " | " + (wheel.Config.SpringRest + wheel.Config.OverExtend) + " | " + lengthChange);

			if (lengthChange + ((wheel.Config.SpringStrength-wheel.Config.SpringDamping)/150000) >= wheel.Config.SpringRest + wheel.Config.OverExtend)
			{
				CarCommon.SuspensionSoundPlayer.Play();
			}
		}
		//--
		
		for (int i = 0; i < wheel.ShapeCast.GetCollisionCount(); i++)
		{
			var contactPoint = wheel.ShapeCast.GetCollisionPoint(i);
			var normal = wheel.ShapeCast.GetCollisionNormal(i);

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
			var forceVector = susForce * normal / wheel.ShapeCast.GetCollisionCount();

			var forcePositionOffset = wheel.GlobalPosition - GlobalPosition;

			ApplyForce(forceVector, forcePositionOffset);

			if (DebugMode)
			{
				DebugDraw3D.DrawArrowRay(contactPoint, forceVector / Mass, 0.5f, arrow_size: 0.1f);
				DebugDraw3D.DrawSphere(contactPoint, wheel.Config.WheelRadius * 0.1f);
			}
		}
	}

	void ProcessAcceleration(CarWheel wheel)
	{
		var forwardDir = wheel.GlobalBasis.Z;
		var carForwardDir = GlobalBasis.Z;
		var velocity = carForwardDir.Dot(LinearVelocity);
		wheel.WheelModel.RotateX((-velocity * (float)GetProcessDeltaTime()) / wheel.Config.WheelRadius);
		
		if (wheel.ShapeCast.IsColliding())
		{
			var forwardStrength = _inputs.Forward;
			var backStrength = -_inputs.Back;
			if (PlayerId < 0 || GameModeController.CurrentGameMode.GetPlayer(PlayerId).State !=
			    GameModeUtils.PLAYER_STATE_PLAYING)
			{
				forwardStrength = 0;
				backStrength = 0;
			}

			//костыль против наскарности дрифткара
			if (forwardStrength == 0 && backStrength == 0 && ReleaseDebuff && velocity > 10)
			{
				forwardStrength = 0.7f;
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

			var accelerationForce = forwardDir * Acceleration * accelerationStrength * accelerationFromCurve;
			var brakingForce = carForwardDir * Acceleration * brakeStrength * accelerationFromCurve * BrakingStrengthMultiplier;
			var contactPoint = wheel.WheelModel.GlobalPosition;
			var forcePosition = contactPoint - GlobalPosition;

			if (wheel.Config.IsDriveWheel)
			{
				ApplyForce(accelerationForce / _driveWheelCount,
					forcePosition);
			}

			ApplyForce(brakingForce / _wheelCount, forcePosition);
			
			if (DebugMode)
			{
				DebugDraw3D.DrawArrowRay(contactPoint, accelerationForce / Mass, 0.5f, Color.Color8(0, 255, 0),
					arrow_size: 0.1f);
				DebugDraw3D.DrawArrowRay(contactPoint, brakingForce / Mass, 0.5f, Color.Color8(255, 000, 0),
					arrow_size: 0.1f);
			}
		}
	}
	
	void SteeringRotation(double delta, CarWheel wheel)
	{
		if (wheel.Config.IsSteeringWheel)
		{
			_targetSteering = 0;
			if (PlayerId >= 0 && GameModeController.CurrentGameMode.GetPlayer(PlayerId).State == GameModeUtils.PLAYER_STATE_PLAYING)
			{
				_targetSteering += _inputs.Left;
				_targetSteering -= _inputs.Right;
				
				_targetSteering *= SpeedSteeringCurve.SampleBaked(
					Mathf.Clamp(
						Mathf.Abs(wheel.GlobalBasis.Z.Dot(LinearVelocity) / MaxSpeed),
						0, 1));
			}

			if (SteeringAffectsCenterOfMass)
			{
				CenterOfMass = new Vector3(_defaultCOM.X + (_targetSteering / 50), _defaultCOM.Y, _defaultCOM.Z);
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
		
		if (wheel.ShapeCast.IsColliding())
		{
			for (int i = 0; i < wheel.ShapeCast.GetCollisionCount(); i++)
			{
				var contactPoint = wheel.ShapeCast.GetCollisionPoint(i);
				var normal = wheel.ShapeCast.GetCollisionNormal(i);

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

				//SkidMarks[wheelId].GlobalPosition = wheel.GetCollisionPoint(0) + Vector3.Up * 0.01f;
				//SkidMarks[wheelId].LookAt(wheel.GlobalPosition + LinearVelocity);

				var handbrake = _isBraking;

				if ((handbrake || grip > SlipThreshold || (wheel.GrassContact && !FullGripOffroad)) && tireVelocity.Length() > 1)
				{
					_isSlipping = true;
				}
				else if (!handbrake && grip < UnslipThreshold)
				{
					_isSlipping = false;
				}

				if (wheel.GrassContact)
				{
					if (!CarCommon.TyreSoundPlayer.Playing || CarCommon.TyreSoundPlayer.Stream != CarCommon.GrassSounds)
					{
						CarCommon.TyreSoundPlayer.Stream = CarCommon.GrassSounds;
						CarCommon.TyreSoundPlayer.VolumeDb = -1;
						CarCommon.TyreSoundPlayer.UnitSize = 3.25f;
						CarCommon.TyreSoundPlayer.Play();
					}
				}
				
				if (_isSlipping)
				{
					xTraction = SlippingTraction;
					wheel.Slide(contactPoint + normal * 0.01f, GetPointVelocity(contactPoint));

					if (wheel.Config.FullLoseGripOnSlip && tireVelocity.Length() > 2 && _targetSteering != 0)
					{
						xTraction = 0;
					}

					if (!wheel.GrassContact && _isAccelerating && _isBraking && wheel.Config.IsDriveWheel && tireVelocity.Length() > 4)
					{
						wheel.SmokeParticles.SetEmitting(true);
					}

					if (!wheel.GrassContact && (!CarCommon.TyreSoundPlayer.Playing || CarCommon.TyreSoundPlayer.Stream != CarCommon.DriftSounds))
					{
						CarCommon.TyreSoundPlayer.Stream = CarCommon.DriftSounds;
						CarCommon.TyreSoundPlayer.VolumeDb = -12;
						CarCommon.TyreSoundPlayer.UnitSize = 10;
						CarCommon.TyreSoundPlayer.Play();
					}
				}
				else
				{
					wheel.StopSliding();
				}
				
				if ((!wheel.GrassContact && !_isSlipping) || LinearVelocity.Length() < 2)
				{
					CarCommon.TyreSoundPlayer.Stop();
				}

				var xForce = -steerSideDirection * steerXVelocity * xTraction * tireWeight;

				var fVelocity = -wheel.GlobalBasis.Z.Dot(tireVelocity);
				var zTraction = WheelZFriction;
				var zForce = wheel.GlobalBasis.Z * fVelocity * zTraction * tireWeight;

				var forcePos = contactPoint - GlobalPosition;
				ApplyForce(xForce / wheel.ShapeCast.GetCollisionCount(), forcePos);
				ApplyForce(zForce / wheel.ShapeCast.GetCollisionCount(), forcePos);
				if (DebugMode)
				{
					DebugDraw3D.DrawArrowRay(contactPoint, xForce / Mass, 0.1f, Color.Color8(0, 0, 255),
						arrow_size: 0.1f);
					DebugDraw3D.DrawArrowRay(contactPoint, zForce / Mass, 0.1f, Color.Color8(0, 0, 255),
						arrow_size: 0.1f);
				}

				if (wheel.GrassContact && tireVelocity.Length() > 5)
				{
					wheel.GrassParticles.SetEmitting(true);
				}
			}
		}
		else
		{
			wheel.StopSliding();
		}
	}

	private void ProcessSpecialBlocks(CarWheel wheel)
	{
		if (wheel.ShapeCast.IsColliding())
		{
			for (int collider = 0; collider < wheel.ShapeCast.GetCollisionCount(); collider++)
			{
				Object collidingObject = wheel.ShapeCast.GetCollider(collider);
				if (collidingObject is StaticBody3D staticBody3D)
				{
					if (staticBody3D.GetOwner() is Block)
					{
						Block block = (Block)(collidingObject as StaticBody3D).GetOwner();
						if (block.WheelTriggerMeshInstance != null && (block.WheelTriggerMeshInstance.GlobalTransform * block.WheelTriggerMeshInstance.GetAabb()).Abs().HasPoint(wheel.ShapeCast.GetCollisionPoint(collider)))
						{
							if (block.IsBooster)
							{
								var force = -block.GlobalBasis.Z * 400;
								ApplyCentralForce(force);
							}
							
							if (block.IsBumper)
							{
								_wheelsOnBump++;
								if (_wheelsOnBump == 1)
								{
									BumpCar(block.GlobalBasis.Y, 1.0f);
								}
							}

							if (block.IsGrass)
							{
								wheel.GrassContact = true;
								wheel.GrassParticles.DrawPass1.SurfaceSetMaterial(0, wheel.ParticlesDefaultMaterial);
							}
						}
					}
					else if (staticBody3D.GetOwner() is TrackBase trackBase)
					{
						wheel.GrassContact = true;
						wheel.GrassParticles.DrawPass1.SurfaceSetMaterial(0, trackBase.ContactParticleMaterial);
					}
				}
			}
		}
	}

	private void ProcessDownForce()
	{
		var forcePosition = GetCenterOfMass();
		var force = -GlobalBasis.Y * MaxDownforce * SpeedToDownforceCurve.SampleBaked(Mathf.Clamp(GlobalBasis.Z.Dot(LinearVelocity) / MaxSpeed, 0, 1));
		ApplyForce(force, forcePosition);
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

		CarCommon.PlayerName.Text = name;
		CarCommon.PlayerName.Visible = PlayerId >= 0 && GameModeController.CurrentGameMode.GetPlayer(PlayerId).Type != GameModeUtils.PLAYER_LOCAL;
		
		void setFontSize(int size)
		{
			Nameplate.Mesh.Set("font_size", size);
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
		
		Nameplate.Mesh.Set("text", name);
	}

	public void SetGhost(bool ghost, int cullLayer = 1)
	{
		IsGhost = ghost;
		
		if (ghost)
		{
			SetPlayerName("");
			CarCommon.PlayerName.Text = "Personal Best";
			CarCommon.InfoSprite.SetLayerMaskValue(1, false);
			CarCommon.InfoSprite.SetLayerMaskValue(cullLayer, true);
		}
		
		foreach (MeshInstance3D mesh in CarModel.GetChildren())
		{
			if (ghost)
			{
				mesh.SetMaterialOverride(ResourceLoader.Load<Material>("res://materials/ghost_car.tres"));
			}
			else
			{
				mesh.SetMaterialOverride(null);		
			}
			
			mesh.SetLayerMaskValue(1, false);
			mesh.SetLayerMaskValue(cullLayer, true);
		}

		MultiplayerSynchronizer.PublicVisibility = !ghost;
		CarCommon.PlayerName.Visible = ghost;
		
		SetCollisionLayerValue(2, false);
	}

	public void SetFrozen(bool frozen)
	{
		_frozen = frozen;
		_frozenPosition = GlobalPosition;
	}
	
	public void TeleportToPoint(Transform3D point)
	{
		SetTransform(point.Orthonormalized());
		LinearVelocity = new Vector3(0, 0, 0);
		AngularVelocity = new Vector3(0, 0, 0);
	}

	public void SetLinearAndAngularVelocities(Vector3 linearVelocity, Vector3 angularVelocity)
	{
		LinearVelocity = linearVelocity;
		AngularVelocity = angularVelocity;
	}

	public void Bonk(Node node)
	{
		if (IsGhost) {return;}
		if (GetLinearVelocity().Length() < 0.05f) {return;}
		if (node is RigidBody3D) {return;}
		
		float avgSpeed = 0;
		for (int i = 0; i < 5; i++)
		{
			float peek = _speedQueue.Dequeue();
			avgSpeed += peek;
			_speedQueue.Enqueue(peek);
		}
		avgSpeed /= 5;
		float speedChange = avgSpeed - GetLinearVelocity().Length();

		CarCommon.CrashSoundPlayer.GlobalPosition = GlobalPosition;
		CarCommon.GrindSoundPlayer.GlobalPosition = GlobalPosition;
		
		if (LinearVelocity.Length() > 2)
		{
			PhysicsDirectBodyState3D state3D = PhysicsServer3D.BodyGetDirectState(GetRid());
			
			for (var contact = 0; contact < state3D.GetContactCount(); contact++)
			{
				if (state3D.GetContactColliderId(contact) == node.GetInstanceId())
				{
					Vector3 position = state3D.GetContactColliderPosition(contact);
					
					//sound pos
					CarCommon.CrashSoundPlayer.GlobalPosition = position;
					CarCommon.GrindSoundPlayer.GlobalPosition = position;
					//
					
					//bonk particles
					foreach (GpuParticles3D particle in CarCommon.CollisionDebrisParticles)
					{
						if (!particle.Emitting)
						{
							particle.SetGlobalPosition(position);
							particle.Emitting = true;

							break;
						}
					}
					//--

					//bumper
					if (node is StaticBody3D staticBody3D && staticBody3D.GetOwner() is Block block)
					{
						if (block.IsBumper && (block.WheelTriggerMeshInstance.GlobalTransform * block.WheelTriggerMeshInstance.GetAabb()).Abs().HasPoint(position))
						{
							BumpCar(block.GlobalBasis.Y, 0.25f);
						}
					}
					//--
				}
			}
		}
		
		//bonk sound
		if (speedChange > MaxSpeed / 15)
		{
			CarCommon.CrashSoundPlayer.Play();
		}
		else if(LinearVelocity.Length() > 2 && !CarCommon.GrindSoundPlayer.Playing)
		{
			CarCommon.GrindSoundPlayer.Play();
			_bonkCount++;
		}
		//--

		//pad vibration
		if (PlayerId >= 0 && GameModeController.CurrentGameMode.GetPlayer(PlayerId) != null && (GameModeController.CurrentGameMode.GetPlayer(PlayerId).Type == GameModeUtils.PLAYER_LOCAL || GameModeController.CurrentGameMode.GetPlayer(PlayerId).Type == GameModeUtils.PLAYER_LOCAL_SPLITSCREEN))
		{
			float magnitude = speedChange / (MaxSpeed / 4);
			float duration = 0.2f;
			if (magnitude > 1.0f) { magnitude = 1.0f; duration += magnitude - 1.0f;}

			if (magnitude > 0)
			{
				InputManager.Instance.VibratePlayer(GameManager.Instance.GetPlayerViewPortById(PlayerId).LocalPlayerId, 0.0f, magnitude, duration);
			}
		}
		//--
	}

	public void Unbonk(Node node)
	{
		if (!IsGhost)
		{
			_bonkCount--;
			if (_bonkCount <= 0)
			{
				_bonkCount = 0;
				CarCommon.GrindSoundPlayer.Stop();
			}
		}
	}

	public void SetSkin(int id)
	{
		if (Skins != null && Skins.Length > 0 && Skins[id] != null)
		{
			MeshInstance3D mesh = (MeshInstance3D)CarModel.GetChildren()[0];
			mesh.SetMaterialOverride(Skins[id]);
			_currentSkin = id;
		}
	}

	public void SetRandomSkin()
	{
		if (Skins != null && Skins.Length > 0)
		{
			_currentSkin = GameManager.Instance.RNG.RandiRange(0, Skins.Length - 1);
			SetSkin(_currentSkin);			
		}
	}

	public void SetOverrideMaterial(Material material)
	{
		MeshInstance3D mesh = (MeshInstance3D)CarModel.GetChildren()[0];
		mesh.SetMaterialOverride(material);
	}

	public void CancelOverrideMaterial()
	{
		SetSkin(_currentSkin);
	}

	public void BumpCar(Vector3 globalBasisY, float multiplier)
	{
		var force = globalBasisY * Mass * 1500 * multiplier;
		force.Y = Mathf.Min(force.Y, 100000);
		ApplyCentralForce(force);
	}
	
	public AudioStream PreloadStreams(AudioStream audioStream)
	{
		if (audioStream is AudioStreamRandomizer audioStreamRandomizer)
		{
			for (int i = 0; i < audioStreamRandomizer.StreamsCount; i++)
			{
				audioStreamRandomizer.SetStream(i, LoadStreamFromPath(audioStreamRandomizer.GetStream(i).ResourcePath));
			}
			
			return audioStreamRandomizer;
		}

		return LoadStreamFromPath(audioStream.ResourcePath);
	}

	private AudioStream LoadStreamFromPath(string path)
	{
		return GD.Load<AudioStream>(path);
	}
}
