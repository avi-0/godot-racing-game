using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;
using racingGame.data;

namespace racingGame;

public partial class GameManager : Node
{
	public enum CarCameraMode
	{
		Orbit,
		Front,
	}
	
	
	[Export] public PackedScene CarScene;
	[Export] public AudioStreamPlayer MusicPlayer;
	[Export] public Track Track;
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
	

	public static GameManager Instance;
	
	// constants that hui znaet where they should be
	public const int BlockLayer = 1;
	public const int CarLayer = 2;
	public const string CarsPath = "res://scenes/cars/";
	
	private bool _isPlaying = false;
	private List<Car> _localCars = new();
	private Dictionary<Car, int> _localPlayerIds = new();
	public bool DirectionalShadowsEnabled = true;

	public Viewport RootViewport;
	private ScreenLayout _screenLayout;
	
	
	public override void _Ready()
	{
		Instance = this;
		
		RootViewport = GetViewport();
		RootViewport.Disable3D = true;
		GetTree().Root.ContentScaleFactor = GuessResolutionScaling();
		
		SetScreenLayout(SingleplayerScreenLayout);

		NewTrack();
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

	public void SelectCarScene(string scenePath)
	{
		CarScene = GD.Load<PackedScene>(CarsPath + scenePath);
	}

	public void Play()
	{
		foreach (var car in _localCars)
		{
			RemoveChild(car);
			car.QueueFree();
			
			_localCars = new();
		}
		
		GameModeController.CurrentGameMode.InitTrack(Track);

		_localPlayerIds = new();
		foreach (var viewport in _screenLayout.PlayerViewports)
		{
			var car = CarScene.Instantiate<Car>();
			_localCars.Add(car);
			
			AddChild(car);
			car.GlobalTransform = GetStartPoint();
			car.Started();

			car.RestartRequested += LocalCarOnRestartRequested;
			car.PauseRequested += LocalCarOnPauseRequested;
		
			car.SetPlayerName(SettingsManager.Instance.GetLocalPlayerName());
			
			if (!_localPlayerIds.ContainsKey(car))
				_localPlayerIds[car] = GameModeController.CurrentGameMode.SpawnPlayer(true, car);
			else
				GameModeController.CurrentGameMode.RespawnPlayer(_localPlayerIds[car], car);

			viewport.Car = car;
		}
		
		_isPlaying = true;
		
		GameModeController.CurrentGameMode.Running(true);

		if (!MusicPlayer.IsPlaying())
			MusicPlayer.Play();
		
		SetViewportsActive(true);
	}

	public Car CreateCar()
	{
		var car = CarScene.Instantiate<Car>();
		AddChild(car);
		return car;
	}

	public void Stop()
	{
		SetViewportsActive(false);
		
		foreach (var car in _localCars)
		{
			RemoveChild(car);
			car.QueueFree();

			_localCars = new();
		}

		_isPlaying = false;

		EmitSignalStoppedPlaying();

		_localPlayerIds = new();
		GameModeController.CurrentGameMode.KillGame();

		MusicPlayer.Stop();
	}

	public void SetViewportsActive(bool visible)
	{
		_screenLayout.Visible = visible;
		foreach (var viewport in _screenLayout.PlayerViewports)
		{
			viewport.Active = visible;
		}
	}

	public bool IsPlaying()
	{
		return _isPlaying;
	}

	private void LocalCarOnPauseRequested()
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

	public Transform3D GetStartPoint()
	{
		foreach (var block in Track.FindChildren("*", "Block", false).Cast<Block>())
			if (block.IsStart)
				return block.SpawnPoint;

		return Transform3D.Identity;
	}

	public void OpenTrack(string path)
	{
		GD.Print($"Opening track at {path}");
		
		Track.Load(Jz.Load<TrackData>(path));

		ApplyShadowSettings();

		GameModeController.CurrentGameMode.InitTrack(Track);
		GD.Print("Track UID: " + GetLoadedTrackUid());
	}

	public void ApplyShadowSettings()
	{
		GetTree().SetGroup("light_directional_shadow", "shadow_enabled", DirectionalShadowsEnabled);
	}

	public void SaveTrack(string path)
	{
		GD.Print($"Saving track as {path}");

		Track.Options.Uid = Guid.NewGuid().ToString();
		
		GD.Print($"New Track UID: {GetLoadedTrackUid()}");
		
		Jz.Save(path, Track.Save());
	}

	public IOrderedEnumerable<string> LoadCarList()
	{
		return ResourceLoader.ListDirectory(CarsPath).ToList().Order();
	}

	public TrackOptions GetTrackOptions(string path)
	{
		try
		{
			var data = Jz.Load<TrackData>(path);

			return data.Options;
		}
		catch (Exception e)
		{
			GD.PushError(e);
			return null;
		}
	}

	public void NewTrack()
	{
		Track.GetNode("Sky3D").GetNode("TimeOfDay").Set("current_time", 10.0f);
		Track.Load(new TrackData());
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

	public string GetLoadedTrackUid()
	{
		return Track.Options.Uid;
	}

	public void NotifyViewportSettingsChanged()
	{
		EmitSignalViewportSettingsChanged();
	}
}
