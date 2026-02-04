using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using racingGame.data;

namespace racingGame;

public partial class GameManager : Node
{
	public static GameManager Instance;
	
	
	public enum CarCameraMode
	{
		Orbit,
		Front,
	}
	
	
	[Export] public AudioStreamPlayer MusicPlayer;
	[Export] public Control PauseMenu;
	[Export] public Control ScreenLayoutSlot;
	[Export] public PanelContainer MOTDPanel;
	[Export] public Label MOTDLabel;
	[Export] public Label PerfLabel;
	[Export] public NotificationContainer NotificationContainer;
	
	[ExportCategory("Screen Layouts")]
	[Export] public PackedScene SingleplayerScreenLayout;
	[Export] public PackedScene SplitScreen2HLayout;
	[Export] public PackedScene SplitScreen2VLayout;
	[Export] public PackedScene SplitScreen3HLayout;
	[Export] public PackedScene SplitScreen3VLayout;
	[Export] public PackedScene SplitScreen4Layout;
	public PackedScene CurrentScreenLayout;
	
	[Signal]
	public delegate void StoppedPlayingEventHandler();

	[Signal]
	public delegate void ViewportSettingsChangedEventHandler();
	
	
	// constants that hui znaet where they should be
	public const int BlockLayer = 1;
	public const int CarLayer = 2;
	public const int DeathY = -9;
	public const int SplitScreenCullMaskStart = 10;

	//more than one splirscreen screen
	public bool IsSplitScreen = false;
	
	private bool _isPlaying = false;

	public Viewport RootViewport;
	private ScreenLayout _screenLayout;

	public RandomNumberGenerator RNG = new RandomNumberGenerator();
	
	public Dictionary<int, FollowEffect> FollowEffects = new Dictionary<int, FollowEffect>();
	
	public override void _Ready()
	{
		Instance = this;
		
		RootViewport = GetViewport();
		RootViewport.Disable3D = true;
		GetTree().Root.ContentScaleFactor = GuessResolutionScaling();
		
		SetScreenLayout(SingleplayerScreenLayout);
		
		MusicPlayer.Finished += PlayNextSong;
	}
	
	public override void _UnhandledInput(InputEvent @event)
	{ 
		if (@event.IsActionPressed(InputActionNames.Pause))
		{
			RootViewport.SetInputAsHandled();
			if (_isPlaying)
			{
				OnPause();
			}
		}
	}

	public override void _Notification(int what)
	{
		//ALT TABBED perf limits
		if (what == NotificationApplicationFocusIn)
		{
			OS.LowProcessorUsageMode = false;
			Engine.MaxFps = 0;
		}
		else if (what == NotificationApplicationFocusOut && !MultiplayerManager.Instance.OnServer)
		{
			OS.LowProcessorUsageMode = true;
			Engine.MaxFps = 20;
		}
		//--
	}

	public void SetScreenLayout(PackedScene layoutScene)
	{
		if (CurrentScreenLayout == layoutScene)
			return;
		CurrentScreenLayout = layoutScene;
		
		if (_screenLayout != null)
		{
			ScreenLayoutSlot.RemoveChild(_screenLayout);
			_screenLayout.QueueFree();
		}

		_screenLayout = layoutScene.Instantiate<ScreenLayout>();
		ScreenLayoutSlot.AddChild(_screenLayout);
		
		MatchViewports();
		
		SetViewportsActive(false);
	}

	public void MatchViewports()
	{
		foreach (var viewport in _screenLayout.PlayerViewports)
		{
			viewport.CullLayer = SplitScreenCullMaskStart + viewport.LocalPlayerId;
			viewport.MatchViewport(RootViewport);
		}
	}

	public void Play(bool host = true)
	{
		MatchViewports();
		
		CarManager.Instance.Clear();

		ClearFollowEffects();
		
		GameModeController.InitGameMode(host);
		GameModeController.LoadMap(TrackManager.Instance.Track);
		
		IsSplitScreen = _screenLayout.PlayerViewports.Count > 1;
		
		bool isFirst = true;
		foreach (var viewport in _screenLayout.PlayerViewports)
		{
			long id = viewport.LocalPlayerId + 1;
			if (isFirst) {id = MultiplayerManager.HOST_ID;}
			
			if (MultiplayerManager.Instance.OnServer)
			{
				id = MultiplayerManager.Instance.PlayerInfo.PlayerId;
			}
			
			viewport.PlayerId = id;
			
			if (isFirst)
			{
				isFirst = false;
				GameModeController.CurrentGameMode.AddPlayer(id, GameModeUtils.PLAYER_LOCAL, SettingsManager.Instance.Settings.PlayerName);
			}
			else
			{
				GameModeController.CurrentGameMode.AddPlayer(id, GameModeUtils.PLAYER_LOCAL_SPLITSCREEN, "Player " + id);
			}

			if (TrackManager.Instance.Track.Options.Message != "")
			{
				ShowMessage(TrackManager.Instance.Track.Options.Message);
			}
			else
			{
				MOTDPanel.Hide();
			}
		}
		
		_isPlaying = true;
		
		GameModeController.CurrentGameMode.Running(true);

		SetViewportsActive(true);
		
		PlayNextSong();
	}

	public void Stop()
	{
		if (!_isPlaying) { return;}
		
		SetViewportsActive(false);
		
		CarManager.Instance.Clear();

		_isPlaying = false;

		GameModeController.UnloadMap(TrackManager.Instance.Track);

		if (MultiplayerManager.Instance.OnServer)
		{
			MultiplayerManager.Instance.TerminateConnection();
		}
		
		EmitSignalStoppedPlaying();
			
		MusicPlayer.Stop();

		ClearFollowEffects();
	}

	public void SetViewportsActive(bool visible)
	{
		_screenLayout.Visible = visible;

		if (!visible)
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}
		
		foreach (var viewport in _screenLayout.PlayerViewports)
		{
			viewport.Active = visible;
		}
	}

	public bool IsPlaying()
	{
		return _isPlaying;
	}

	private void OnPause()
	{
		if (!PauseMenu.Visible)
		{
			PauseMenu.Show();
		}
	}

	public void LocalCarOnRestartRequested()
	{
		Play();
	}

	private float GuessResolutionScaling()
	{
		if (OS.HasFeature("windows"))
		{
			var height = DisplayServer.ScreenGetSize().Y;
			return height / 1080.0f;
		}

		return DisplayServer.ScreenGetScale(); // only works on macOS and Linux
	}

	public void NotifyViewportSettingsChanged()
	{
		EmitSignalViewportSettingsChanged();
	}

	public void ShowMessage(string msg)
	{
		NotificationContainer.DisplayMessage(msg);
	}
	private void OnMOTDButtonPressed()
	{
		MOTDPanel.Hide();
		Input.MouseMode = Input.MouseModeEnum.Hidden;
	}

	public void PlayNextSong()
	{
		if (!MusicPlayer.IsPlaying())
		{
			MusicPlayer.Play();
			GD.Print(MusicPlayer.Stream.GetName());
		}		
	}

	public PlayerViewport GetPlayerViewPortById(long id)
	{
		foreach (PlayerViewport port in _screenLayout.PlayerViewports)
		{
			if (port.PlayerId == id)
			{
				return port;
			}
		}

		return null;
	}
	
	public void SyncCameraVisuals(Camera3D camera, int cameraCullLayer)
	{
		if (!FollowEffects.ContainsKey(cameraCullLayer))
		{
			FollowEffects[cameraCullLayer] = TrackManager.Instance.Track.FeScene.Instantiate<FollowEffect>();
			TrackManager.Instance.Track.AddChild(FollowEffects[cameraCullLayer]);
		}
		
		for (int cull = SplitScreenCullMaskStart; cull < SplitScreenCullMaskStart + 8; cull++)
		{
			if (cull != cameraCullLayer)
			{
				camera.SetCullMaskValue(cull, false);
			}
			else
			{
				camera.SetCullMaskValue(cull, true);
			}
		}
		
		FollowEffects[cameraCullLayer].Weather.Emitting = TrackManager.Instance.Track.Options.Rain;
		FollowEffects[cameraCullLayer].Weather.SetGlobalPosition(new Vector3(camera.GlobalPosition.X, camera.GlobalPosition.Y+15, camera.GlobalPosition.Z));
		FollowEffects[cameraCullLayer].Weather.SetLayerMaskValue(cameraCullLayer, true);
		
		FollowEffects[cameraCullLayer].Water.SetGlobalPosition(new Vector3(camera.GlobalPosition.X, FollowEffects[cameraCullLayer].Water.GlobalPosition.Y, camera.GlobalPosition.Z));
		FollowEffects[cameraCullLayer].Water.SetLayerMaskValue(cameraCullLayer, true);
		
		if (!TrackManager.Instance.Track.Options.Rain)
		{
			FollowEffects[cameraCullLayer].CollisionHeightField.Dispose();
		}
		else
		{
			FollowEffects[cameraCullLayer].CollisionHeightField.SetGlobalPosition(new Vector3(camera.GlobalPosition.X, camera.GlobalPosition.Y + 20, camera.GlobalPosition.Z));
		}
	}

	private void ClearFollowEffects()
	{
		foreach (KeyValuePair<int, FollowEffect> kv in FollowEffects)
		{
			TrackManager.Instance.Track.RemoveChild(kv.Value);
			kv.Value.Water.Dispose();
			kv.Value.Weather.Dispose();
			kv.Value.CollisionHeightField.Dispose();
			kv.Value.Dispose();
		}
		
		FollowEffects.Clear();
	}
}
