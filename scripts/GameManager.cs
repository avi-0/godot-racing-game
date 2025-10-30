using Godot;
using System;
using System.Linq;
using racingGame;

public partial class GameManager : Node
{
    public static GameManager Singleton;

    // constants that hui znaet where they should be
    public const int BlockLayer = 1;
    public const int CarLayer = 2;

    [Export(PropertyHint.FilePath)] public string TrackTemplatePath;

    public const string CarsPath = "res://scenes/cars/";

    [Export] public PackedScene CarScene;

    [Export] public Control PauseMenu;

    [Export] public Control SettingsMenu;

    [Export] public Node3D TrackNode;

    [Export] public Label TimeLabel;
    [Export] public Label SpeedLabel;
    [Export] public Label PBLabel;

    [Export] public Panel FinishPanel;
    [Export] public Label FinishTimeLabel;
    
    private bool _isPlaying = false;

    private Car _localCar = null;

    private int LocalPlayerID = -1;

    [Signal]
    public delegate void StoppedPlayingEventHandler();

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
        CarScene = GD.Load<PackedScene>(CarsPath+scenePath);
    }

    public void Play()
    {
        SetGameUIVisiblity(true);

        if (_localCar != null)
        {
            RemoveChild(_localCar);
            _localCar.QueueFree();
        }
        
        _localCar = CarScene.Instantiate<Car>();
        AddChild(_localCar);
        _localCar.GlobalTransform = GetStartPoint();
        _localCar.ResetPhysicsInterpolation(); // doesnt help the wheels lol
        _localCar.Started();
        
        _localCar.RestartRequested += LocalCarOnRestartRequested;
        _localCar.PauseRequested += LocalCarOnPauseRequested;
        
        foreach (var block in TrackNode.FindChildren("*", "Block").Cast<Block>())
        {
            if (block.IsFinish)
            {
                block.CarEntered += FinishOnCarEntered;
            }
        }

        _isPlaying = true;

        if (LocalPlayerID == -1)
        {
            LocalPlayerID = GameMode.CurrentGameMode.SpawnPlayer(true, _localCar);
        }
        else
        {
            GameMode.CurrentGameMode.RespawnPlayer(LocalPlayerID);
        }

        GameMode.CurrentGameMode.Running(true);
    }

    public void Stop()
    {
        SetGameUIVisiblity(false);

        if (_localCar != null)
        {
            RemoveChild(_localCar);
            _localCar.QueueFree();
            
            _localCar = null;
        }
        
        foreach (var block in TrackNode.FindChildren("*", "Block").Cast<Block>())
        {
            if (block.IsFinish)
            {
                block.CarEntered -= FinishOnCarEntered;
            }
        }
        
        _isPlaying = false;
        
        EmitSignalStoppedPlaying();

        LocalPlayerID = -1;
        GameMode.CurrentGameMode.KillGame();
    }
    
    private void SetGameUIVisiblity(bool Visible)
    {
        TimeLabel.Visible = Visible;
        SpeedLabel.Visible = Visible;
        PBLabel.Visible = Visible;

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
        var node = GetTree().GetFirstNodeInGroup("start_point");
        if (node is Node3D node3d)
        {
            return node3d.GlobalTransform;
        }
        
        return Transform3D.Identity;
    }

    public void SaveTrack(string path)
    {
        GD.Print($"Saving track as {path}");
        
        foreach (var child in TrackNode.GetChildren())
        {
            child.Owner = TrackNode;
        }

        var scene = new PackedScene();
        GD.Print($"Packing: {scene.Pack(TrackNode)}");
        GD.Print($"Saving: {ResourceSaver.Save(scene, path)}");
    }

    public IOrderedEnumerable<string> LoadCarList()
    {
        return ResourceLoader.ListDirectory(CarsPath).ToList().Order();
    }

    public void OpenTrack(string path)
    {
        GD.Print($"Opening track at {path}");

        var trackName = path.Split("/")[path.Split("/").Length-1];
        if (LoadCarList().Contains(trackName.Split("_")[0]+".tscn"))
        {
            SelectCarScene(trackName.Split("_")[0]+".tscn");
        }

        var scene = ResourceLoader.Load<PackedScene>(path, cacheMode: ResourceLoader.CacheMode.Ignore);
        var newTrackNode = scene.Instantiate<Node3D>();
        
        TrackNode.AddSibling(newTrackNode);
        TrackNode.GetParent().RemoveChild(TrackNode);
        TrackNode.QueueFree();
        TrackNode = newTrackNode;
        TrackNode.Name = "Track";

        GameMode.CurrentGameMode.InitTrack(TrackNode);
    }

    private void FinishOnCarEntered(Car car)
    {
        GameMode.CurrentGameMode.PlayerAttemptFinish(0);
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
}
