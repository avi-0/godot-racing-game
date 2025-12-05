using Godot;
using System;
using racingGame;
using racingGame.data;

public partial class PlayerViewport : SubViewport
{
	[Export] public Label CheckPointLabel;
	[Export] public PanelContainer FinishPanel;
	[Export] public Label FinishTimeLabel;
	[Export] public Label LapsLabel;
	[Export] public Label PbLabel;
	[Export] public Control RaceUi;
	[Export] public Label SpeedLabel;
	[Export] public Label StartTimerLabel;
	[Export] public Label TimeLabel;
	[Export] public Label TrackInfoLabel;
	[Export] public Camera3D Camera;
	[Export] public int LocalPlayerId = 0;
	
	public GameManager.CarCameraMode CameraMode = GameManager.CarCameraMode.Orbit;
	public Car Car;
	public int StartTimerSeconds = -1;

	private CarInputs _inputs;
	private bool _active = false;

	public bool Active
	{
		get => _active;
		set
		{
			_active = value;
			ProcessMode = _active ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
			FinishPanel.Visible = false;
		}
	}
	
	private Camera3D TargetCamera
	{
		get
		{
			if (CameraMode == GameManager.CarCameraMode.Orbit)
				return Car?.OrbitCamera.Camera;
			if (CameraMode == GameManager.CarCameraMode.Front)
				return Car?.FrontCamera;

			return null;
		}
	}


	public override void _Ready()
	{
		SizeChanged += OnSizeChanged;
		OnSizeChanged();

		GameManager.Instance.ViewportSettingsChanged += OnViewportSettingsChanged;
	}

	public override void _ExitTree()
	{
		GameManager.Instance.ViewportSettingsChanged -= OnViewportSettingsChanged;
	}

	private void OnSizeChanged()
	{
		var size = (Vector2) Size;
		size *= Math.Max(1920f / size.X, 1080f / size.Y);
		Size2DOverride = (Vector2I) size.Round();
		Size2DOverrideStretch = true;
	}

	private void OnViewportSettingsChanged()
	{
		this.MatchViewport(GameManager.Instance.RootViewport);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!Active || Car == null)
			return;
		
		UpdateCarInputs();
		
		SpeedLabel.Text = ((int)Mathf.Round(Car.LinearVelocity.Length() * 10)).ToString();
		
		Camera.Current = TargetCamera != null;
		Camera.Match(TargetCamera);
	}

	public override void _Process(double delta)
	{
		if (Active && Car != null)
		{
			GameModeController.CurrentGameMode.UpdateHud(this);
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!InputManager.Instance.InputEventMatchesPlayer(@event, LocalPlayerId))
			return;

		if (@event.IsAction(InputActionNames.Forward, true))
		{
			_inputs.Forward = @event.GetActionStrength(InputActionNames.Forward, true);
		} 
		else if (@event.IsAction(InputActionNames.Back, true))
		{
			_inputs.Back = @event.GetActionStrength(InputActionNames.Back, true);
		} 
		else if (@event.IsAction(InputActionNames.Left, true))
		{
			_inputs.Left = @event.GetActionStrength(InputActionNames.Left, true);
		} 
		else if (@event.IsAction(InputActionNames.Right, true))
		{
			_inputs.Right = @event.GetActionStrength(InputActionNames.Right, true);
		} 
		else if (@event.IsActionPressed(InputActionNames.CycleCamera))
		{
			if (CameraMode == GameManager.CarCameraMode.Orbit)
			{
				CameraMode = GameManager.CarCameraMode.Front;
			}
			else
			{
				CameraMode = GameManager.CarCameraMode.Orbit;
			}
			SetInputAsHandled();
		}
		else if (@event.IsActionPressed(InputActionNames.Restart))
		{
			Car.InputRestart();
			SetInputAsHandled();
		}
		else if (@event.IsActionPressed(InputActionNames.Pause))
		{
			Car.InputPause();
			SetInputAsHandled();
		}
		else if(@event.IsActionPressed(InputActionNames.ToggleLights))
		{
			Car.InputToggleLights();
			SetInputAsHandled();
		}
	}

	private void UpdateCarInputs()
	{
		Car.SetInputs(_inputs);
	}

	private void OnFinishButtonPressed()
	{
		FinishPanel.Hide();
		Input.MouseMode = Input.MouseModeEnum.Hidden;
		GameManager.Instance.LocalCarOnRestartRequested();
	}
}
