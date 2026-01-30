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
	private Dictionary<long, Car> _playerCarsById = new();
	
	
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
		car.SetSkin(0);
		AddChild(car);
		return car;
	}

	public void Clear()
	{
		foreach (var car in _cars)
		{
			RemoveChild(car);
			car.QueueFree();
		}
		
		_cars = new();
		_playerCarsById = new();
	}

	public Car GetPlayerCarById(long id)
		=> _playerCarsById.GetValueOrDefault(id);

	public void DestroyPlayerCar(long id)
	{
		if (_playerCarsById.GetValueOrDefault(id) is Car car)
		{
			RemoveChild(car);
			_cars.Remove(car);
			
			car.QueueFree();
		}
	}

	public Car CreatePlayerCar(long id)
	{
		DestroyPlayerCar(id);

		var car = CarScene.Instantiate<Car>();

		car.Name = id.ToString();
		if (MultiplayerManager.Instance.OnServer)
		{
			car.SetMultiplayerAuthority((int)id);
		}
		
		_cars.Add(car);
		_playerCarsById[id] = car;
		
		AddChild(car, true);
		car.GlobalTransform = TrackManager.Instance.GetStartPoint();
		car.Started();
		
		car.SetPlayerName(SettingsManager.Instance.GetLocalPlayerName());

		if (SettingsManager.Instance.Settings.SelectedSkins.ContainsKey(car.CarName) && SettingsManager.Instance.Settings.SelectedSkins[car.CarName] > 0 && car.Skins[SettingsManager.Instance.Settings.SelectedSkins[car.CarName]] != null)
		{
			car.SetSkin(SettingsManager.Instance.Settings.SelectedSkins[car.CarName]);
		}
		else
		{
			car.SetSkin(0);
		}
		
		return car;
	}
}
