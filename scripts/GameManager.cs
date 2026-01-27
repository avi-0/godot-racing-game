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

	//more than one splirscreen screen
	public bool IsSplitScreen = false;
	
	private bool _isPlaying = false;

	public Viewport RootViewport;
	private ScreenLayout _screenLayout;

	public RandomNumberGenerator RNG = new RandomNumberGenerator();
	
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
		else if (what == NotificationApplicationFocusOut)
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
		
		foreach (var viewport in _screenLayout.PlayerViewports)
		{
			viewport.MatchViewport(RootViewport);
		}
		
		SetViewportsActive(false);
	}

	public void Play(bool host = true)
	{
		CarManager.Instance.Clear();
		GameModeController.CurrentGameMode.InitTrack(TrackManager.Instance.Track);
		
		IsSplitScreen = _screenLayout.PlayerViewports.Count > 1;
		
		bool isFirst = true;
		foreach (var viewport in _screenLayout.PlayerViewports)
		{
			long id = viewport.LocalPlayerId + 1;
			if (MultiplayerManager.Instance.OnServer)
			{
				id = MultiplayerManager.Instance.PlayerInfo.PlayerId;
			}

			string name = "Player " + id;
			if (isFirst)
			{
				name = SettingsManager.Instance.GetLocalPlayerName();
			}
			GameModeController.CurrentGameMode.AddPlayer(id, true, isFirst, name);
			isFirst = false;
			
			viewport.PlayerId = id;

			if (TrackManager.Instance.Track.Options.Message != "")
			{
				MOTDLabel.Text = TrackManager.Instance.Track.Options.Message;
				MOTDPanel.Show();
				Input.MouseMode = Input.MouseModeEnum.Visible;
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
		SetViewportsActive(false);
		
		CarManager.Instance.Clear();

		_isPlaying = false;

		GameModeController.CurrentGameMode.KillGame();

		if (MultiplayerManager.Instance.OnServer)
		{
			MultiplayerManager.Instance.TerminateConnection();
		}
		
		EmitSignalStoppedPlaying();
			
		MusicPlayer.Stop();
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
}
