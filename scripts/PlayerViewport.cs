using Godot;
using System;
using racingGame;

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

	public GameManager.CarCameraMode CameraMode = GameManager.CarCameraMode.Orbit;
	public Car Car;

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
	

	private void OnViewportSettingsChanged()
	{
		this.MatchViewport(GameManager.Singleton.RootViewport);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!Active || Car == null)
			return;
		
		SpeedLabel.Text = ((int)Mathf.Round(Car.LinearVelocity.Length() * 10)).ToString();
		
		Camera.Current = TargetCamera != null;
		Camera.Match(TargetCamera);
	}

	public override void _Process(double delta)
	{
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event.IsActionPressed(InputActionNames.CycleCamera))
		{
			if (CameraMode == GameManager.CarCameraMode.Orbit)
			{
				CameraMode = GameManager.CarCameraMode.Front;
			}
			else
			{
				CameraMode = GameManager.CarCameraMode.Orbit;
			}
			GetViewport().SetInputAsHandled();
		}
	}
}
