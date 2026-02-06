using System;
using Godot;
using racingGame.data;

namespace racingGame;

public partial class PlayerViewport : SubViewport
{
	[Export] public Label CheckPointLabel;
	[Export] public PanelContainer FinishPanel;
	[Export] public RichTextLabel FinishTimeLabel;
	[Export] public Label LapsLabel;
	//[Export] public Label PbLabel;
	[Export] public Control RaceUi;
	[Export] public Label SpeedLabel;
	[Export] public Label StartTimerLabel;
	[Export] public Label TimeLabel;
	[Export] public Label TrackInfoLabel;
	[Export] public VBoxContainer ScoreboardContainer;
	[Export] public Camera3D Camera;
	[Export] public int LocalPlayerId = 0;
	[Export] public TextureRect SpeedArrow;
	
	public GameManager.CarCameraMode CameraMode = GameManager.CarCameraMode.Orbit;
	public long PlayerId;
	public int StartTimerSeconds = -1;
	
	public int CullLayer = 0;
	
	private CarInputs _inputs;
	private bool _active = false;

	private int _defaultFov = 80;
	
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

	public Car Car => CarManager.Instance.GetPlayerCarById(PlayerId);

	private void OnViewportSettingsChanged()
	{
		this.MatchViewport(GameManager.Instance.RootViewport);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!Active || Car == null)
			return;
		
		UpdateCarInputs();

		int speed = (int)Mathf.Round(Car.LinearVelocity.Length() * 8);
		
		int maxRange = 283 - 14;
		float ratio = speed / 650.0f;
		SpeedArrow.RotationDegrees = 14 + (maxRange * ratio);
		
		SpeedLabel.Text = speed.ToString();
		if (speed > 999) { SpeedLabel.Text = "???";}
		
		Camera.Current = TargetCamera != null;
		Camera.Match(TargetCamera);
		
		if (Camera.Current)
		{
			GameManager.Instance.SyncCameraVisuals(Camera, CullLayer);
			Camera.Fov = _defaultFov;
			if (speed > 70)
			{
				Camera.Fov += (speed-70) / 15.0f;
				if (Camera.Fov > 120) { Camera.Fov = 120.0f; }
			}
		}
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
			GameModeUtils.RestartPlayer(PlayerId);
			SetInputAsHandled();
		}
		else if (@event.IsActionPressed(InputActionNames.Respawn))
		{
			GameModeController.CurrentGameMode.RespawnPlayer(PlayerId);
			//SetInputAsHandled();
		}
		else if(@event.IsActionPressed(InputActionNames.ToggleLights))
		{
			Car.InputToggleLights();
			SetInputAsHandled();
		}
		else if (@event.IsActionPressed(InputActionNames.HideUI))
		{
			RaceUi.Visible = !RaceUi.Visible;
			SetInputAsHandled();
		}
		else if (@event.IsActionPressed(InputActionNames.HideGhost))
		{
			if (GameModeController.CurrentGameMode.GetPlayer(PlayerId).PlayerGhostCar != null)
			{
				GameModeController.CurrentGameMode.GetPlayer(PlayerId).PlayerGhostCar.Visible = !GameModeController.CurrentGameMode.GetPlayer(PlayerId).PlayerGhostCar.Visible;

				if (GameModeController.CurrentGameMode.GetPlayer(PlayerId).Type == GameModeUtils.PLAYER_LOCAL)
				{
					SettingsManager.Instance.Settings.GhostVisible = GameModeController.CurrentGameMode.GetPlayer(PlayerId).PlayerGhostCar.Visible;
				}
			}
			SetInputAsHandled();
		}
	}

	private void UpdateCarInputs()
	{
		Car.SetInputs(_inputs);
	}

	private void OnFinishButtonPressed()
	{
		GameModeUtils.RestartPlayer(PlayerId);
		FinishPanel.Hide();
		Input.MouseMode = Input.MouseModeEnum.Hidden;
	}
}