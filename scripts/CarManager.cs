using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace racingGame;

public partial class CarManager : Node
{
	public static CarManager Instance;
	
	
	public const string CarsPath = "res://scenes/cars/";
	
	
	[Export] public PackedScene CarScene;
	
	
	private List<Car> _localCars = new();
	private Dictionary<Car, int> _localPlayerIds = new();
	
	
	
	public override void _Ready()
	{
		Instance = this;
	}
	
	public IOrderedEnumerable<string> LoadCarList()
	{
		return ResourceLoader.ListDirectory(CarsPath).ToList().Order();
	}
	
	public void SelectCarScene(string scenePath)
	{
		CarScene = GD.Load<PackedScene>(CarsPath + scenePath);
	}
	
	public Car CreateCar()
	{
		var car = CarScene.Instantiate<Car>();
		AddChild(car);
		return car;
	}

	public void Clear()
	{
		foreach (var car in _localCars)
		{
			RemoveChild(car);
			car.QueueFree();
			
			_localCars = new();
		}
		
		_localPlayerIds = new();
	}

	public Car CreatePlayerCar()
	{
		var car = CarScene.Instantiate<Car>();
		_localCars.Add(car);
			
		AddChild(car);
		car.GlobalTransform = TrackManager.Instance.GetStartPoint();
		car.Started();
		
		car.SetPlayerName(SettingsManager.Instance.GetLocalPlayerName());
			
		if (!_localPlayerIds.ContainsKey(car))
			_localPlayerIds[car] = GameModeController.CurrentGameMode.SpawnPlayer(true, car);
		else
			GameModeController.CurrentGameMode.RespawnPlayer(_localPlayerIds[car], car);

		return car;
	}
}
