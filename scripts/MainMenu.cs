using System.Linq;
using Fractural.Tasks;
using Godot;

namespace racingGame;

public partial class MainMenu : Control
{
	public string CampTracksPath = "res://tracks/";
	public string UserTracksPath = "user://tracks/";
	[Export] public FileDialog MenuFileDialog;
	
	[Export] public Control TrackListPanel;
	[Export] public GridContainer TrackContainer;
	[Export] public Control GarageWindow;
	[Export] public TextureRect GarageTextureRect;
	[Export] public SubViewport GarageViewport;
	[Export] public Container GarageContainer;

	private Car _loadedCar;
	private IOrderedEnumerable<string> _carList;
	
	public override void _Ready()
	{
		Editor.Singleton.IsRunning = false;
	}

	public override void _Process(double delta)
	{
		if (GarageWindow.Visible)
		{
			GarageTextureRect.Texture = GarageViewport.GetTexture();
		}
	}

	public void OnPlayButtonPressed()
	{
		FillTrackContainer(CampTracksPath);
		TrackListPanel.Show();
	}

	public void OnEditorButtonPressed()
	{
		OpenEditor().Forget();
	}

	public void OnLoadButtonPressed()
	{
		FillTrackContainer(UserTracksPath);
		TrackListPanel.Show();
	}

	public void OnGarageButton()
	{
		GarageWindow.Visible = !GarageWindow.Visible;
		GarageViewport.Size = DisplayServer.WindowGetSize();
		
		if (GarageWindow.Visible)
		{
			_carList = GameManager.Singleton.LoadCarList();
			
			LoadGarageCar(GameManager.CarsPath + _carList.First());
			
			foreach (var car in _carList)
			{
				var button = new Button();
				button.CustomMinimumSize = 64 * Vector2.One;
				button.Text = car;
				button.Pressed += () => LoadGarageCar(GameManager.CarsPath + car);

				GarageContainer.AddChild(button);
			}
		}
		else
		{
			GarageContainer.DestroyAllChildren();
			_loadedCar.QueueFree();
			_loadedCar = null;
		}
	}

	private void LoadGarageCar(string path)
	{
		GarageViewport.DestroyAllChildren();
		_loadedCar = GD.Load<PackedScene>(path).Instantiate<Car>();
		GarageViewport.AddChild(_loadedCar);
		Input.MouseMode = Input.MouseModeEnum.Visible;
		_loadedCar.OrbitCamera.CameraStickBase.RotationDegrees = new Vector3(0, 215, 0);
		_loadedCar.OrbitCamera.Camera.SetFov(80);
	}

	public void OnSettingsButtonPressed()
	{
		GameManager.Singleton.SettingsMenu.Show();
	}

	public void OnExitButtonPressed()
	{
		GetTree().Root.PropagateNotification((int)NotificationWMCloseRequest);
		GetTree().Quit();
	}
	
	public void OnTrackListBackButton()
	{
		TrackListPanel.Hide();
		TrackContainer.DestroyAllChildren();
	}

	private async GDTaskVoid OpenEditor()
	{
		Visible = false;

		GameManager.Singleton.NewTrack();
		Editor.Singleton.IsRunning = true;
		Editor.Singleton.SetupOptions();

		await GDTask.ToSignal(Editor.Singleton, Editor.SignalName.Exited);

		Visible = true;
	}

	private async GDTaskVoid OpenTrack(string path)
	{
		Visible = false;

		GameManager.Singleton.OpenTrack(path);
		GameManager.Singleton.Play();

		await GDTask.ToSignal(GameManager.Singleton, GameManager.SignalName.StoppedPlaying);

		Visible = true;
	}
	
	private void FillTrackContainer(string basePath)
	{
		var trackList = LoadTrackList(basePath);
		
		foreach (var trackPath in trackList)
		{
			var trackMeta = GameManager.Singleton.GetTrackMetadata(basePath + trackPath);
			if (trackMeta.ContainsKey("AuthorTime") && trackMeta["AuthorTime"] != "0")
			{
				var button = new Button();
				button.CustomMinimumSize = 64 * Vector2.One;
				button.Text = trackMeta["TrackName"];
				button.Pressed += () => OpenTrack(basePath + trackPath).Forget();

				TrackContainer.AddChild(button);
			}
		}
	}
	private IOrderedEnumerable<string> LoadTrackList(string path)
	{
		return ResourceLoader.ListDirectory(path).ToList().Order();
	}
}