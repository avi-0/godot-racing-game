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
	[Export] public Label CheckSplit;
	
	public GameManager.CarCameraMode CameraMode = GameManager.CarCameraMode.Orbit;
	public long PlayerId;
	public int StartTimerSeconds = -1;
	
	public int CullLayer = 0;

	public float MouseSensitivity = 0.005f;
	
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

		int playerState = GameModeController.CurrentGameMode.GetPlayer(PlayerId).State;
		if (playerState == GameModeUtils.PLAYER_STATE_PLAYING)
		{
			UpdateCarInputs();
		}
		
		int speed = (int)Mathf.Round(Car.LinearVelocity.Length() * 3.6f);
		
		int maxRange = 283 - 14;
		float ratio = speed / 300.0f;
		SpeedArrow.RotationDegrees = 14 + (maxRange * ratio);
		
		SpeedLabel.Text = speed.ToString();
		if (speed > 999) { SpeedLabel.Text = "???";}

		if (playerState == GameModeUtils.PLAYER_STATE_PLAYING || playerState == GameModeUtils.PLAYER_STATE_PRESTART || playerState == GameModeUtils.PLAYER_STATE_DEAD || playerState == GameModeUtils.PLAYER_STATE_AFTERFINISH)
		{
			Camera.Current = TargetCamera != null;
			Camera.Match(TargetCamera);
			Car.CarCommon.AudioListener.GlobalPosition = Camera.GlobalPosition;

			if (Camera.Current)
			{
				GameManager.Instance.SyncCameraVisuals(Camera, CullLayer);
				Camera.SetFov(_defaultFov);
				if (speed > 70)
				{
					Camera.SetFov(_defaultFov + ((speed - 70) / 15.0f));
					if (Camera.GetFov() > 120)
					{
						Camera.SetFov(120.0f);
					}
				}
			}
		}
		else if (playerState == GameModeUtils.PLAYER_STATE_WALKING)
		{
			GameModeController.CurrentGameMode.GetPlayer(PlayerId).WalkingPlayer.SetInputs(_inputs);
			
			Camera.Match(GameModeController.CurrentGameMode.GetPlayer(PlayerId).WalkingPlayer.Camera);
			Car.CarCommon.AudioListener.GlobalPosition = Camera.GlobalPosition;

			if (Camera.Current)
			{
				GameManager.Instance.SyncCameraVisuals(Camera, CullLayer);
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

	public override void _Input(InputEvent @event)
	{
		if (!Active || Car == null || GameManager.Instance.PauseMenu.Visible) { return;}
		
		if (!InputManager.Instance.InputEventMatchesPlayer(@event, LocalPlayerId))
			return;
		
		//control camera with mouse - RMB bind
		if (@event is InputEventMouseButton mouseButtonEvent)
		{
			if (mouseButtonEvent.IsActionPressed(InputActionNames.MoveCamera))
			{
				Car.IsPlayerMouseControllingCamera = true;
				SetInputAsHandled();
			}
			else if (mouseButtonEvent.IsActionReleased(InputActionNames.MoveCamera))
			{
				Car.IsPlayerMouseControllingCamera = false;
				//SetInputAsHandled();
			}
		}
		//--
	}
	public override void _UnhandledInput(InputEvent @event)
	{
		if (!Active || Car == null || GameManager.Instance.PauseMenu.Visible) { return;}
		
		if (!InputManager.Instance.InputEventMatchesPlayer(@event, LocalPlayerId))
			return;

		int state = GameModeController.CurrentGameMode.GetPlayer(PlayerId).State;
		
		//control camera with mouse
		if (@event is InputEventMouseMotion mouseMotionEvent)
		{
			if (Car.IsPlayerMouseControllingCamera)
			{
				Car.OrbitCamera.RotateCameraX(-mouseMotionEvent.Relative.X * MouseSensitivity);
			}

			if (state == GameModeUtils.PLAYER_STATE_WALKING)
			{
				GameModeController.CurrentGameMode.GetPlayer(PlayerId).WalkingPlayer.RotateY(-mouseMotionEvent.Relative.X * MouseSensitivity);
				GameModeController.CurrentGameMode.GetPlayer(PlayerId).WalkingPlayer.Camera.RotateX(-mouseMotionEvent.Relative.Y * MouseSensitivity);

				if (GameModeController.CurrentGameMode.GetPlayer(PlayerId).WalkingPlayer.Camera.Rotation.X > 1.0f)
				{
					GameModeController.CurrentGameMode.GetPlayer(PlayerId).WalkingPlayer.Camera.Rotation = new Vector3(1.0f, 0, 0);
				} 
				else if (GameModeController.CurrentGameMode.GetPlayer(PlayerId).WalkingPlayer.Camera.Rotation.X < -1.0f)
				{
					GameModeController.CurrentGameMode.GetPlayer(PlayerId).WalkingPlayer.Camera.Rotation = new Vector3(-1.0f, 0, 0);
				}
			}
			
			//SetInputAsHandled();
			return;
		}
		//--

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
			SetInputAsHandled();
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
		else if (@event.IsActionPressed(InputActionNames.MoveCameraLeft))
		{
			Car.IsPlayerPadControllingCamera = true;
			_inputs.PadCamLeft = @event.GetActionStrength(InputActionNames.MoveCameraLeft, true);
		}
		else if (@event.IsActionPressed(InputActionNames.MoveCameraRight))
		{
			Car.IsPlayerPadControllingCamera = true;
			_inputs.PadCamRight = @event.GetActionStrength(InputActionNames.MoveCameraRight, true);
		}
		else if (@event.IsActionReleased(InputActionNames.MoveCameraLeft))
		{
			Car.IsPlayerPadControllingCamera = false;
			_inputs.PadCamLeft = 0;
			_inputs.PadCamRight = 0;
		}
		else if (@event.IsActionReleased(InputActionNames.MoveCameraRight))
		{
			Car.IsPlayerPadControllingCamera = false;
			_inputs.PadCamLeft = 0;
			_inputs.PadCamRight = 0;
		}
		else if (@event.IsActionPressed(InputActionNames.ExitCar))
		{
			GameModeController.CurrentGameMode.PlayerAttemptsToExitCar(PlayerId);
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