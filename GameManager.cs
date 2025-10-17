using Godot;
using System;
using racingGame;

public partial class GameManager : Node
{
    public static GameManager __Instance;
    
    [Export] public PackedScene CarScene;
    
    private bool _isPlaying = false;

    private Car _localCar = null;

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

        _isPlaying = true;
    }

    private void LocalCarOnRestartRequested()
    {
        Play();
    }
}
