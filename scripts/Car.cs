using System.Collections.Generic;
using Godot;

namespace racingGame;

public partial class Car : VehicleBody3D
{
	[Signal]
	public delegate void PauseRequestedEventHandler();

	[Signal]
	public delegate void RestartRequestedEventHandler();

	private const float MouseSens = 1.0f;
	private const float PitchMaxSpeed = 500f;
	private readonly Dictionary<VehicleWheel3D, float> _wheelTargetFriction = new();

	private float _brakeFwSlip;
	private float _brakeRwSlip;

	private bool _isLocallyControlled = true;

	private float _mouseSensitivity;
	[Export] public float BrakeForce = 2.00f;
	[Export] public float BrakeForceInTurnMultiplier = 0.25f;
	[Export] public float BrakeFwSlipMultiplier = 1.5f;
	[Export] public float BrakeRwSlipMultiplier = 0.25f;
	[Export] public Camera3D Camera;
	[Export] public Node3D CameraStick;
	[Export] public Node3D CameraStickBase;

	[Export] public float EngineForceForward = 90.0f;
	[Export] public AudioStreamPlayer3D EngineSound;
	[Export] public float NormalRwSlip = 3.9f;
	[Export] public float NormalSlip = 4.0f;

	public int PlayerId;
	[Export] public Curve SkidToFrictionCurve;
	[Export] public Curve SpeedToEngineMultCurve;
	[Export] public Curve SpeedToPitchCurve;
	[Export] public Curve SpeedToSteeringCurve;
	[Export] public float SteeringSpeed = 400.0f;
	[Export] public VehicleWheel3D WheelBl;
	[Export] public VehicleWheel3D WheelBr;

	[Export] public VehicleWheel3D WheelFl;
	[Export] public VehicleWheel3D WheelFr;

	public bool IsLocallyControlled
	{
		get => _isLocallyControlled;
		set
		{
			Camera.Current = value;
			_isLocallyControlled = value;
		}
	}

	public bool AcceptsInputs { get; set; } = false;

	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;

		_brakeFwSlip = NormalSlip * BrakeFwSlipMultiplier;
		_brakeRwSlip = NormalRwSlip * BrakeRwSlipMultiplier;
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
		var speediness = GetSpeediness();
		EngineSound.PitchScale = SpeedToPitchCurve.Sample(Mathf.Abs(speediness));

		GameManager.Singleton.SpeedLabel.Text = ((int)Mathf.Round(LinearVelocity.Length() * 10)).ToString();

		EngineForce = 0;
		var engineSoundTarget = 0.5f;
		if (Input.IsActionPressed("throttle") && AcceptsInputs)
		{
			EngineForce = EngineForceForward * (speediness > 0 ? SpeedToEngineMultCurve.Sample(speediness) : 1.0f);
			engineSoundTarget = 1.0f;
		}

		EngineSound.VolumeDb = Mathf.LinearToDb(
			Mathf.MoveToward(Mathf.DbToLinear(EngineSound.VolumeDb), engineSoundTarget, 2 * (float)delta)
		);

		float targetSteering = 0;
		if (AcceptsInputs)
		{
			if (Input.IsActionPressed("steer_left"))
				targetSteering += 1;
			if (Input.IsActionPressed("steer_right"))
				targetSteering -= 1;
		}

		targetSteering *= Mathf.DegToRad(SpeedToSteeringCurve.Sample(Mathf.Abs(speediness)));
		Steering = Mathf.MoveToward(Steering, targetSteering, Mathf.DegToRad(SteeringSpeed) * (float)delta);

		_wheelTargetFriction[WheelFl] = NormalSlip;
		_wheelTargetFriction[WheelFr] = NormalSlip;
		_wheelTargetFriction[WheelBl] = NormalRwSlip;
		_wheelTargetFriction[WheelBr] = NormalRwSlip;
		Brake = 0;
		if (Input.IsActionPressed("brake") && AcceptsInputs)
		{
			var applySlip = false;
			if (GetRpm() > 10)
			{
				if (targetSteering == 0)
				{
					Brake = BrakeForce;
				}
				else
				{
					Brake = BrakeForce * BrakeForceInTurnMultiplier;
					applySlip = true;
				}
			}
			else
			{
				EngineForce = -EngineForceForward;
			}

			if (applySlip)
			{
				_wheelTargetFriction[WheelFl] = _brakeFwSlip;
				_wheelTargetFriction[WheelFr] = _brakeFwSlip;
				_wheelTargetFriction[WheelBl] = _brakeRwSlip;
				_wheelTargetFriction[WheelBr] = _brakeRwSlip;
			}
		}


		UpdateWheel(WheelFl, (float)delta);
		UpdateWheel(WheelFr, (float)delta);
		UpdateWheel(WheelBl, (float)delta);
		UpdateWheel(WheelBr, (float)delta);
	}

	public override void _IntegrateForces(PhysicsDirectBodyState3D state)
	{
		var maxAngularSpeed = 3.0f;
		if (state.AngularVelocity.Length() > maxAngularSpeed)
			state.AngularVelocity = state.AngularVelocity.Normalized() * maxAngularSpeed;
		//GD.Print("limiting angular velocity");
	}

	private float GetRpm()
	{
		var speedLeft = WheelBl.GetRpm();
		var speedRight = WheelBr.GetRpm();
		return (speedLeft + speedRight) / 2;
	}

	private float GetSpeediness()
	{
		var rpm = GetRpm();
		rpm /= PitchMaxSpeed;
		return Mathf.Clamp(rpm, -1, 1);
	}

	private void UpdateWheel(VehicleWheel3D wheel, float delta)
	{
		var target = _wheelTargetFriction[wheel];
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