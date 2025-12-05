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
	
	private bool _isPlaying = false;

	public Viewport RootViewport;
	private ScreenLayout _screenLayout;
	
	
	public override void _Ready()
	{
		Instance = this;
		
		RootViewport = GetViewport();
		RootViewport.Disable3D = true;
		GetTree().Root.ContentScaleFactor = GuessResolutionScaling();
		
		SetScreenLayout(SingleplayerScreenLayout);
	}

	public override void _UnhandledInput(InputEvent @event)
	{ 
		if (@event.IsActionPressed(InputActionNames.Pause))
		{
			RootViewport.SetInputAsHandled();
			OnPause();
		}
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

	public void Play()
	{
		CarManager.Instance.Clear();
		
		GameModeController.CurrentGameMode.InitTrack(TrackManager.Instance.Track);
		
		foreach (var viewport in _screenLayout.PlayerViewports)
		{
			var car = CarManager.Instance.CreatePlayerCar();
			
			car.RestartRequested += LocalCarOnRestartRequested;

			viewport.Car = car;
		}
		
		_isPlaying = true;
		
		GameModeController.CurrentGameMode.Running(true);

		if (!MusicPlayer.IsPlaying())
			MusicPlayer.Play();
		
		SetViewportsActive(true);
	}

	public void Stop()
	{
		SetViewportsActive(false);
		
		CarManager.Instance.Clear();

		_isPlaying = false;

		EmitSignalStoppedPlaying();
		
		GameModeController.CurrentGameMode.KillGame();

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
}
