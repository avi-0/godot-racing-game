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
	[Export] public SubViewport GarageViewport;
	[Export] public Container GarageContainer;

	private Car _loadedCar;
	private IOrderedEnumerable<string> _carList;
	
	public override void _Ready()
	{
		Editor.Singleton.IsRunning = false;
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
			GarageViewport.MatchViewport(GetViewport());
			
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
		_loadedCar.OrbitCamera.Yaw = float.DegreesToRadians(215);
		_loadedCar.OrbitCamera.Pitch = float.DegreesToRadians(30);
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
			var options = GameManager.Singleton.GetTrackOptions(basePath + trackPath);
			GD.Print(options);
			
			if (options == null)
				continue;
			
			if (options.AuthorTime > 0)
			{
				var button = new Button();
				button.CustomMinimumSize = 64 * Vector2.One;
				button.Text = options.Name;
				button.Pressed += () => OpenTrack(basePath + trackPath).Forget();
				
				Image image = new Image();
				if (image.LoadJpgFromBuffer(Marshalls.Base64ToRaw(options.PreviewImage)) != Error.Ok)
				{
					image = Image.CreateEmpty(512, 512, true, Image.Format.Rgb8);
				}
				
				image.Resize(320, 320, Image.Interpolation.Lanczos);
				
				button.SetButtonIcon(ImageTexture.CreateFromImage(image));
				
				TrackContainer.AddChild(button);
			}
		}
	}
	private IOrderedEnumerable<string> LoadTrackList(string path)
	{
		return DirAccess.Open(path)
			.GetFiles()
			.Where(file => file.EndsWith(".tk.jz"))
			.ToList().Order();
	}
}