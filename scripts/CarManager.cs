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
	
	
	private List<Car> _cars = new();
	private Dictionary<Guid, Car> _playerCarsById = new();
	
	
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
		foreach (var car in _cars)
		{
			RemoveChild(car);
			car.QueueFree();
			
			_cars = new();
		}
		
		_playerCarsById = new();
	}

	public Car GetPlayerCarById(Guid id)
		=> _playerCarsById.GetValueOrDefault(id);

	public void DestroyPlayerCar(Guid id)
	{
		if (_playerCarsById.GetValueOrDefault(id) is Car car)
		{
			RemoveChild(car);
			_cars.Remove(car);
			
			car.QueueFree();
		}
	}

	public Car CreatePlayerCar(Guid id)
	{
		DestroyPlayerCar(id);
		
		var car = CarScene.Instantiate<Car>();
		_cars.Add(car);
		_playerCarsById[id] = car;
			
		AddChild(car);
		car.GlobalTransform = TrackManager.Instance.GetStartPoint();
		car.Started();
		
		car.SetPlayerName(SettingsManager.Instance.GetLocalPlayerName());

		return car;
	}
}
