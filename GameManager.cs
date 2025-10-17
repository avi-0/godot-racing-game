using Godot;
using System;
using racingGame;

public partial class GameManager : Node
{
    public static GameManager __Instance;
    
    [Export] public PackedScene CarScene;
    
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
}
