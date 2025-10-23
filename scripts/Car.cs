using Godot;
using System.Collections.Generic;

namespace racingGame;

public partial class Car : VehicleBody3D
{
    private const float MouseSens = 1.0f;
    private const float EngineForceForward = 90.0f;
    private const float DriftBonus = 1.0f;
    private const float BrakeForce = 0.5f;
    private const float NormalSlip = 2.0f;
    private const float NormalRwSlip = NormalSlip * 0.965f;
    private const float BrakeFwSlip = NormalSlip * 0.2f;
    private const float BrakeRwSlip = NormalRwSlip * 0.20f;
    private const float SteeringSpeed = 200.0f;
    private const float PitchMaxSpeed = 500f;
    
    [Signal]
    public delegate void RestartRequestedEventHandler();

    [Signal]
    public delegate void PauseRequestedEventHandler();
    
    [Export] public VehicleWheel3D WheelFl { get; set; }
    [Export] public VehicleWheel3D WheelFr { get; set; }
    [Export] public VehicleWheel3D WheelBl { get; set; }
    [Export] public VehicleWheel3D WheelBr { get; set; }
    [Export] public Camera3D Camera { get; set; }
    [Export] public Node3D CameraStick { get; set; }
    [Export] public Node3D CameraStickBase { get; set; }
    [Export] public AudioStreamPlayer3D EngineSound { get; set; }
    [Export] public Curve SpeedToPitchCurve { get; set; }
    [Export] public Curve SpeedToSteeringCurve { get; set; }
    [Export] public Curve SkidToFrictionCurve { get; set; }

    
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
    
    private float _mouseSensitivity;
    private Dictionary<VehicleWheel3D, float> _wheelTargetFriction = new();
    
    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
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
        
        if (@event is InputEventKey keyEvent && keyEvent.IsReleased())
        {
            if (keyEvent.PhysicalKeycode == Key.Escape)
            {
                if (Input.MouseMode == Input.MouseModeEnum.Captured)
                {
                    Input.MouseMode = Input.MouseModeEnum.Visible;
                    EmitSignalPauseRequested();
                }
                    
                else
                    Input.MouseMode = Input.MouseModeEnum.Captured;
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
        EngineForce = 0;
        float engineSoundTarget = 0.5f;
        if (Input.IsActionPressed("throttle"))
        {
            EngineForce = EngineForceForward;
            engineSoundTarget = 1.0f;
        }
        EngineSound.VolumeDb = Mathf.LinearToDb(
            Mathf.MoveToward(Mathf.DbToLinear(EngineSound.VolumeDb), engineSoundTarget, 2 * (float)delta)
        );
        
        float speediness = GetSpeediness();
        EngineSound.PitchScale = SpeedToPitchCurve.Sample(Mathf.Abs(speediness));
        
        _wheelTargetFriction[WheelFl] = NormalSlip;
        _wheelTargetFriction[WheelFr] = NormalSlip;
        _wheelTargetFriction[WheelBl] = NormalRwSlip;
        _wheelTargetFriction[WheelBr] = NormalRwSlip;
        Brake = 0;
        if (Input.IsActionPressed("brake"))
        {
            bool applySlip = false;
            if (!Input.IsActionPressed("throttle"))
            {
                if (GetRpm() > 10)
                {
                    Brake = BrakeForce;
                    applySlip = true;
                }
                else
                {
                    EngineForce = -EngineForceForward;
                }
            }
            else
            {
                EngineForce *= DriftBonus;
                applySlip = true;
            }
            
            if (applySlip)
            {
                _wheelTargetFriction[WheelFl] = BrakeFwSlip;
                _wheelTargetFriction[WheelFr] = BrakeFwSlip;
                _wheelTargetFriction[WheelBl] = BrakeRwSlip;
                _wheelTargetFriction[WheelBr] = BrakeRwSlip;
            }
        }
        
        float targetSteering = 0;
        if (Input.IsActionPressed("steer_left"))
            targetSteering += 1;
        if (Input.IsActionPressed("steer_right"))
            targetSteering -= 1;
        targetSteering *= Mathf.DegToRad(SpeedToSteeringCurve.Sample(Mathf.Abs(speediness)));
        Steering = Mathf.MoveToward(Steering, targetSteering, Mathf.DegToRad(SteeringSpeed) * (float)delta);
        
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
            GD.Print("limiting angular velocity");
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