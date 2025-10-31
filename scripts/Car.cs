using Godot;
using System.Collections.Generic;

namespace racingGame;

public partial class Car : VehicleBody3D
{	
	[Signal]
	public delegate void RestartRequestedEventHandler();

	[Signal]
	public delegate void PauseRequestedEventHandler();

	private const float MouseSens = 1.0f;
	private const float PitchMaxSpeed = 500f;

	[Export] public VehicleWheel3D WheelFl;
	[Export] public VehicleWheel3D WheelFr;
	[Export] public VehicleWheel3D WheelBl;
	[Export] public VehicleWheel3D WheelBr;
	[Export] public Camera3D Camera;
	[Export] public Node3D CameraStick;
	[Export] public Node3D CameraStickBase;
	[Export] public AudioStreamPlayer3D EngineSound;
	[Export] public Curve SpeedToPitchCurve;
	[Export] public Curve SpeedToSteeringCurve;
	[Export] public Curve SkidToFrictionCurve;
	[Export] public Curve SpeedToEngineMultCurve;

	[Export] public float EngineForceForward = 90.0f;
	[Export] public float BrakeForce = 2.00f;
	[Export] public float NormalSlip = 4.0f;
	[Export] public float NormalRwSlip = 3.9f;
	[Export] public float SteeringSpeed = 400.0f;
	[Export] public float BrakeFwSlipMultiplier = 1.5f;
	[Export] public float BrakeRwSlipMultiplier = 0.25f;
	[Export] public float BrakeForceInTurnMultiplier = 0.25f;

	private float BrakeFwSlip;
	private float BrakeRwSlip;

	public int PlayerID;
	
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

	private bool _acceptsInputs = false;
	public bool AcceptsInputs
	{
		get => _acceptsInputs;
		set
		{
			_acceptsInputs = value;
		}
	}	
	
	private float _mouseSensitivity;
	private Dictionary<VehicleWheel3D, float> _wheelTargetFriction = new();
	
	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;

		BrakeFwSlip = NormalSlip * BrakeFwSlipMultiplier;
		BrakeRwSlip = NormalRwSlip * BrakeRwSlipMultiplier;
	}
	
	public override void _Process(double delta)
	{
		_mouseSensitivity = MouseSens * 0.25f * 2 * Mathf.Pi / DisplayServer.ScreenGetSize().Y;
		
		ControlCamera();
		UpdateCameraYaw((float)delta);
		//GD.Print(GetRpm());
	}
	
	public override void _UnhandledInput(InputEvent @event)
	{
		if (!IsLocallyControlled)
			return;

		if (@event is InputEventKey keyEvent && keyEvent.IsPressed())
		{
			if (keyEvent.PhysicalKeycode == Key.Escape)
			{
				EmitSignalPauseRequested();
			}
			else if (keyEvent.PhysicalKeycode == Key.R)
			{
				EmitSignalRestartRequested();
			}
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
		float speediness = GetSpeediness();
		EngineSound.PitchScale = SpeedToPitchCurve.Sample(Mathf.Abs(speediness));
		
		GameManager.Singleton.SpeedLabel.Text = Mathf.Floor(LinearVelocity.Length()*5).ToString();

		EngineForce = 0;
		float engineSoundTarget = 0.5f;
		if (Input.IsActionPressed("throttle") && AcceptsInputs)
		{
			EngineForce = EngineForceForward * (speediness > 0 ? SpeedToEngineMultCurve.Sample(speediness) : 1.0f);
			engineSoundTarget = 1.0f;
		}
		EngineSound.VolumeDb = Mathf.LinearToDb(
			Mathf.MoveToward(Mathf.DbToLinear(EngineSound.VolumeDb), engineSoundTarget, 2 * (float)delta)
		);
		
		float targetSteering = 0;
		if (Input.IsActionPressed("steer_left"))
			targetSteering += 1;
		if (Input.IsActionPressed("steer_right"))
			targetSteering -= 1;
		targetSteering *= Mathf.DegToRad(SpeedToSteeringCurve.Sample(Mathf.Abs(speediness)));
		Steering = Mathf.MoveToward(Steering, targetSteering, Mathf.DegToRad(SteeringSpeed) * (float)delta);

		_wheelTargetFriction[WheelFl] = NormalSlip;
		_wheelTargetFriction[WheelFr] = NormalSlip;
		_wheelTargetFriction[WheelBl] = NormalRwSlip;
		_wheelTargetFriction[WheelBr] = NormalRwSlip;
		Brake = 0;
		if (Input.IsActionPressed("brake") && AcceptsInputs)
		{
			bool applySlip = false;
			if (GetRpm() > 10)
			{
				if (targetSteering == 0)
				{
					Brake = BrakeForce;
				}
				else
				{
					Brake = BrakeForce*BrakeForceInTurnMultiplier;
					applySlip = true;
				}
			}
			else
			{
				EngineForce = -EngineForceForward;
			}
			
			if (applySlip)
			{
				_wheelTargetFriction[WheelFl] = BrakeFwSlip;
				_wheelTargetFriction[WheelFr] = BrakeFwSlip;
				_wheelTargetFriction[WheelBl] = BrakeRwSlip;
				_wheelTargetFriction[WheelBr] = BrakeRwSlip;
			}
		}
		

		UpdateWheel(WheelFl, (float)delta);
		UpdateWheel(WheelFr, (float)delta);
		UpdateWheel(WheelBl, (float)delta);
		UpdateWheel(WheelBr, (float)delta);
	}
	
	public override void _IntegrateForces(PhysicsDirectBodyState3D state)
	{
		float maxAngularSpeed = 3.0f;
		if (state.AngularVelocity.Length() > maxAngularSpeed)
		{
			state.AngularVelocity = state.AngularVelocity.Normalized() * maxAngularSpeed;
			//GD.Print("limiting angular velocity");
		}
	}
	
	private float GetRpm()
	{
		float speedLeft = WheelBl.GetRpm();
		float speedRight = WheelBr.GetRpm();
		return (speedLeft + speedRight) / 2;
	}
	
	private float GetSpeediness()
	{
		float rpm = GetRpm();
		rpm /= PitchMaxSpeed;
		return Mathf.Clamp(rpm, -1, 1);
	}
	
	private void UpdateWheel(VehicleWheel3D wheel, float delta)
	{
		float target = _wheelTargetFriction[wheel];
		if (!wheel.IsInContact())
			target = 0;
		
		wheel.WheelFrictionSlip = target;
		//wheel.WheelFrictionSlip = Mathf.MoveToward(wheel.WheelFrictionSlip, target, FrictionAdjSpeed * delta);
		//wheel.WheelFrictionSlip = target * SkidToFrictionCurve.Sample(wheel.GetSkidinfo());
	}

	private float GetCameraTargetYaw(Vector3 dir)
	{
		return -dir.Slide(Vector3.Up).SignedAngleTo(Vector3.Back, Vector3.Up);
	}
	
	private void UpdateCameraYaw(float delta)
	{
		if (LinearVelocity.Length() > 1.0f)
		{
			float target = GetCameraTargetYaw(LinearVelocity);
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
		{
			CameraStick.Rotation = new Vector3(
				CameraStick.Rotation.X,
				Mathf.Pi,
				CameraStick.Rotation.Z
			);
		}
	}
}
