using Godot;
using System;
using racingGame;

public partial class GameManager : Node
{
    public static GameManager __Instance;
    
    [Export] public PackedScene CarScene;

    [Export] public Node3D TrackNode;
    
    private bool _isPlaying = false;

    private Car _localCar = null;

    [Signal]
    public delegate void StoppedPlayingEventHandler();

    public override void _Ready()
    {
        __Instance = this;
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
}
