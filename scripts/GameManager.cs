using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace racingGame;

public partial class GameManager : Node
{
	[Signal]
	public delegate void StoppedPlayingEventHandler();

	// constants that hui znaet where they should be
	public const int BlockLayer = 1;
	public const int CarLayer = 2;

	public const string CarsPath = "res://scenes/cars/";
	public static GameManager Singleton;

	private bool _isPlaying = false;

	private NewCar _localCar = null;

	private int _localPlayerId = -1;

	[Export] public PackedScene CarScene;
	[Export] public Label CheckPointLabel;

	public Dictionary<string, string> CurrentTrackMeta;

	[Export] public PanelContainer FinishPanel;
	[Export] public Label FinishTimeLabel;
	[Export] public Label LapsLabel;

	[Export] public AudioStreamPlayer MusicPlayer;

	[Export] public Control PauseMenu;
	[Export] public Label PbLabel;

	[Export] public Control RaceUi;

	[Export] public Control SettingsMenu;
	[Export] public Label SpeedLabel;
	[Export] public Label StartTimerLabel;
	[Export] public Label TimeLabel;
	[Export] public Label TrackInfoLabel;

	[Export] public Node3D TrackNode;

	[Export(PropertyHint.FilePath)] public string TrackTemplatePath;

	public override void _Ready()
	{
		Singleton = this;

		GetTree().Root.ContentScaleFactor = GuessResolutionScaling();

		NewTrack();
	}

	public override void _Process(double delta)
	{
	}

	public void SelectCarScene(string scenePath)
	{
		CarScene = GD.Load<PackedScene>(CarsPath + scenePath);
	}

	public void Play()
	{
		SetGameUiVisiblity(true);

		if (_localCar != null)
		{
			RemoveChild(_localCar);
			_localCar.QueueFree();
		}

		_localCar = CarScene.Instantiate<NewCar>();
		AddChild(_localCar);
		_localCar.GlobalTransform = GetStartPoint();
		_localCar.ResetPhysicsInterpolation(); // doesnt help the wheels lol
		_localCar.Started();

		_localCar.RestartRequested += LocalCarOnRestartRequested;
		_localCar.PauseRequested += LocalCarOnPauseRequested;

		_isPlaying = true;

		if (_localPlayerId == -1)
			_localPlayerId = GameModeController.CurrentGameMode.SpawnPlayer(true, _localCar);
		else
			GameModeController.CurrentGameMode.RespawnPlayer(_localPlayerId, _localCar);

		GameModeController.CurrentGameMode.Running(true);

		if (!MusicPlayer.IsPlaying())
			MusicPlayer.Play();
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
		//CurrentTrackMeta = null;

		MusicPlayer.Stop();
	}

	private void SetGameUiVisiblity(bool visible)
	{
		RaceUi.Visible = visible;

		FinishPanel.Hide();
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

	private void LocalCarOnRestartRequested()
	{
		Play();
	}

	private Transform3D GetStartPoint()
	{
		foreach (var block in TrackNode.FindChildren("*", "Block", false).Cast<Block>())
			if (block.IsStart)
				return block.SpawnPoint;

		return Transform3D.Identity;
	}

	public void SaveTrack(string path)
	{
		GD.Print($"Saving track as {path}");

		foreach (var key in CurrentTrackMeta.Keys) TrackNode.SetMeta(key, CurrentTrackMeta[key]);
		TrackNode.SetMeta("TrackUID", Guid.NewGuid().ToString());
		GD.Print($"New Track UID: {GetLoadedTrackUid()}");

		foreach (var child in TrackNode.GetChildren()) child.Owner = TrackNode;

		var scene = new PackedScene();
		GD.Print($"Packing: {scene.Pack(TrackNode)}");
		GD.Print($"Saving: {ResourceSaver.Save(scene, path)}");
	}

	public IOrderedEnumerable<string> LoadCarList()
	{
		return ResourceLoader.ListDirectory(CarsPath).ToList().Order();
	}

	public Dictionary<string, string> GetTrackMetadata(string path)
	{
		var returnList = new Dictionary<string, string>();

		var scenestate = ResourceLoader.Load<PackedScene>(path, cacheMode: ResourceLoader.CacheMode.Ignore).GetState();
		for (var propertyId = 0; propertyId < scenestate.GetNodePropertyCount(0); propertyId++)
		{
			string propertyName = scenestate.GetNodePropertyName(0, propertyId);
			if (propertyName.Contains("metadata/"))
			{
				propertyName = propertyName.Replace("metadata/", "");
				returnList.Add(propertyName, (string)scenestate.GetNodePropertyValue(0, propertyId));
				GD.Print(propertyName + " " + returnList[propertyName]);
			}
		}

		return returnList;
	}

	public void OpenTrack(string path)
	{
		GD.Print($"Opening track at {path}");

		CurrentTrackMeta = GetTrackMetadata(path);

		var scene = ResourceLoader.Load<PackedScene>(path, cacheMode: ResourceLoader.CacheMode.Ignore);
		var newTrackNode = scene.Instantiate<Node3D>();

		TrackNode.AddSibling(newTrackNode);
		TrackNode.GetParent().RemoveChild(TrackNode);
		TrackNode.QueueFree();
		TrackNode = newTrackNode;
		TrackNode.Name = "Track";

		GameModeController.CurrentGameMode.InitTrack(TrackNode);
		GD.Print("Track UID: " + GetLoadedTrackUid());
	}

	public void OnFinishButtonPressed()
	{
		FinishPanel.Hide();
		Input.MouseMode = Input.MouseModeEnum.Captured;
		LocalCarOnRestartRequested();
	}

	public void NewTrack()
	{
		OpenTrack(TrackTemplatePath);
	}

	private float GuessResolutionScaling()
	{
		if (OS.HasFeature("windows"))
		{
			var height = DisplayServer.WindowGetSize().Y;
			return height / 1080.0f;
		}

		return DisplayServer.ScreenGetScale(); // only works on macOS and Linux
	}

	public string GetLoadedTrackUid()
	{
		return CurrentTrackMeta["TrackUID"];
	}
}
