using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Newtonsoft.Json;

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
	[Export] public SettingsMenu SettingsMenu;
	[Export] public Control RaceUi;
	[Export] public Control PauseMenu;
	[Export] public PlayerViewport PlayerViewport;
	[Export] public MainMenu MainMenu;
	
	[Signal]
	public delegate void StoppedPlayingEventHandler();
	

	// constants that hui znaet where they should be
	public const int BlockLayer = 1;
	public const int CarLayer = 2;
	public const string CarsPath = "res://scenes/cars/";
	public static GameManager Singleton;
	private bool _isPlaying = false;
	private Car _localCar = null;
	private int _localPlayerId = -1;
	public bool DirectionalShadowsEnabled = true;

	public Viewport RootViewport;

	
	
	public override void _Ready()
	{
		Singleton = this;
		
		RootViewport = GetViewport();
		GetTree().Root.ContentScaleFactor = GuessResolutionScaling();

		NewTrack();
	}

	public void SelectCarScene(string scenePath)
	{
		CarScene = GD.Load<PackedScene>(CarsPath + scenePath);
	}

	public void Play()
	{
		if (_localCar != null)
		{
			RemoveChild(_localCar);
			_localCar.QueueFree();
		}

		_localCar = CarScene.Instantiate<Car>();
		AddChild(_localCar);
		_localCar.GlobalTransform = GetStartPoint();
		_localCar.Started();

		_localCar.RestartRequested += LocalCarOnRestartRequested;
		_localCar.PauseRequested += LocalCarOnPauseRequested;
		
		GD.Print(SettingsMenu.GetLocalPlayerName());
		_localCar.SetPlayerName(SettingsMenu.GetLocalPlayerName());
		
		_isPlaying = true;

		if (_localPlayerId == -1)
			_localPlayerId = GameModeController.CurrentGameMode.SpawnPlayer(true, _localCar);
		else
			GameModeController.CurrentGameMode.RespawnPlayer(_localPlayerId, _localCar);

		GameModeController.CurrentGameMode.Running(true);

		if (!MusicPlayer.IsPlaying())
			MusicPlayer.Play();
		
		SetGameUiVisiblity(true);
	}

	public void Stop()
	{
		SetGameUiVisiblity(false);

		if (_localCar != null)
		{
			RemoveChild(_localCar);
			_localCar.QueueFree();

			_localCar = null;
		}

		_isPlaying = false;

		EmitSignalStoppedPlaying();

		_localPlayerId = -1;
		GameModeController.CurrentGameMode.KillGame();

		MusicPlayer.Stop();
	}

	public void SetGameUiVisiblity(bool visible)
	{
		RaceUi.Visible = visible;
		PlayerViewport.Active = visible;
		PlayerViewport.Car = _localCar;
	}

	public bool IsPlaying()
	{
		return _isPlaying;
	}

	private void LocalCarOnPauseRequested()
	{
		if (PauseMenu.Visible)
		{
			PauseMenu.Hide();
			Input.MouseMode = Input.MouseModeEnum.Captured;
		}
		else
		{
			PauseMenu.Show();
			Input.MouseMode = Input.MouseModeEnum.Visible;
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
		PlayerViewport.MatchViewport(RootViewport);
		MainMenu.GarageViewport.MatchViewport(RootViewport);
	}
}
