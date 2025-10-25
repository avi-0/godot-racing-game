using Godot;
using System;
using racingGame;

public partial class GameManager : Node
{
    public static GameManager Singleton;
    
    [Export] public PackedScene CarScene;

    [Export] public Node3D TrackNode;
    
    private bool _isPlaying = false;

    private Car _localCar = null;

    [Signal]
    public delegate void StoppedPlayingEventHandler();

    public override void _Ready()
    {
        Singleton = this;
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
        _localCar.ResetPhysicsInterpolation(); // doesnt help the wheels lol
        _localCar.Started();
        
        _localCar.RestartRequested += LocalCarOnRestartRequested;
        _localCar.PauseRequested += LocalCarOnPauseRequested;

        _isPlaying = true;
    }

    private void LocalCarOnPauseRequested()
    {
        Stop();
    }

    public void Stop()
    {
        if (_localCar != null)
        {
            RemoveChild(_localCar);
            _localCar.QueueFree();
            
            _localCar = null;
        }
        
        _isPlaying = false;
        
        EmitSignalStoppedPlaying();
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

    public void OpenTrack(string path)
    {
        GD.Print($"Opening track at {path}");

        var scene = ResourceLoader.Load<PackedScene>(path, cacheMode: ResourceLoader.CacheMode.Ignore);
        var newTrackNode = scene.Instantiate<Node3D>();
        
        TrackNode.AddSibling(newTrackNode);
        TrackNode.GetParent().RemoveChild(TrackNode);
        TrackNode.QueueFree();
        TrackNode = newTrackNode;
        TrackNode.Name = "Track";
    }
}
