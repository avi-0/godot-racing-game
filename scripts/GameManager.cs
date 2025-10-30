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
    
    private bool _isPlaying = false;

    private Car _localCar = null;

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
        TimeLabel.Text = "";
        if (_isPlaying == true)
        {   
            var RaceTime = DateTime.Now.Subtract(RaceStartTime);
            TimeLabel.Text =  RaceTime.ToString("mm") + ":" + RaceTime.ToString("ss") + "." + RaceTime.ToString("fff");
        }   
    }

    private DateTime RaceStartTime = DateTime.Now;

    public void SelectCarScene(string scenePath)
    {
        CarScene = GD.Load<PackedScene>(CarsPath+scenePath);
    }

    public void Play()
    {
        SpeedLabel.Visible = true;

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

        RaceStartTime = DateTime.Now;
    }

    public void Stop()
    {
        SpeedLabel.Visible = false;

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
    }

    private void FinishOnCarEntered(Car car)
    {
        Play(); // рестарт
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
