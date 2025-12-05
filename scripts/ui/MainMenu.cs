using System;
using System.Collections.Generic;
using System.Linq;
using Fractural.Tasks;
using Godot;
using racingGame.data;
using racingGame.extensions;

namespace racingGame;

public partial class MainMenu : Control
{
	public string CampTracksPath = "res://tracks/";
	public string UserTracksPath = "user://tracks/";
	[Export] public FileDialog MenuFileDialog;

	[Export] public Button PlayButton;
	[Export] public Button SettingsButton;
	[Export] public Control TrackListPanel;
	[Export] public GridContainer TrackContainer;
	[Export] public Control MainMenuContainer;
	[Export] public FoldableContainer SplitscreenFoldableContainer;
	[Export] public Control GarageWindow;
	[Export] public CustomSubViewportContainer GarageViewportContainer;
	[Export] public SubViewport GarageViewport;
	[Export] public Node3D GarageNode;
	[Export] public Node3D GarageCameraBase;
	[Export] public Container GarageContainer;
	[Export] public LineEdit PlayerNameText;
	[Export] public Control CampaignControl;
	[Export] public Container CampaignContainer;
	
	[Export(PropertyHint.FilePath)] public string DefaultCarPath;

	private Car _loadedCar;
	private IOrderedEnumerable<string> _carList;
	private Control _hadFocus;

	public bool IsVisible
	{
		get => Visible;
		set
		{
			Visible = value;
			GarageViewportContainer.Visible = value;
			GarageNode.Visible = value;
		}
	}
	
	private List<Campaign> _campaigns = new List<Campaign>();
	
	public override void _Ready()
	{
		Editor.Singleton.IsRunning = false;

		SettingsButton.Pressed += () => OnSettingsButtonPressed().Forget();
		SplitscreenFoldableContainer.Hidden += () => SplitscreenFoldableContainer.Folded = true;
		
		_carList = GameManager.Singleton.LoadCarList();
		LoadGarageCar(DefaultCarPath);
		
		AddCampaign("Tutorial", "tutorial");
		AddCampaign("Main Campaign", "main");
		
		PlayButton.CallDeferred("grab_focus");
	}

	private void OnViewportSettingsChanged()
	{
		GarageViewport.MatchViewport(GameManager.Singleton.RootViewport);
	}

	public void OnPlayButtonPressed()
	{
		_hadFocus = GetViewport().GuiGetFocusOwner();
		
		foreach (Campaign campaign in _campaigns)
		{
			var button = new Button();
			button.CustomMinimumSize = 64 * Vector2.One;
			button.Text = campaign.Name;
			button.Pressed += () =>
			{
				FillTrackContainer(CampTracksPath + campaign.DirectoryName + "/");
				TrackListPanel.Show();
			};

			CampaignContainer.AddChild(button);		
		}
	
		CampaignControl.Show();
	}

	public void OnEditorButtonPressed()
	{
		OpenEditor().Forget();
	}

	public void OnLoadButtonPressed()
	{
		_hadFocus = GetViewport().GuiGetFocusOwner();
		
		FillTrackContainer(UserTracksPath);
		TrackListPanel.Show();
	}

	public void OnGarageButton()
	{
		GarageWindow.Visible = !GarageWindow.Visible;
		
		if (GarageWindow.Visible)
		{
			MainMenuContainer.Visible = false;
			
			foreach (var car in _carList)
			{
				var button = new Button();
				button.CustomMinimumSize = 64 * Vector2.One;
				button.Text = car;
				button.Pressed += () => LoadGarageCar(GameManager.CarsPath + car);

				GarageContainer.AddChild(button);
			}

			_hadFocus = GetViewport().GuiGetFocusOwner();
			GarageContainer.GetChild<Control>(0).GrabFocus();

			PlayerNameText.Text = GameManager.Singleton.SettingsMenu.GetLocalPlayerName();
		}
		else
		{
			MainMenuContainer.Visible = true;
			
			GarageContainer.DestroyAllChildren();
			
			if (_hadFocus != null)
				_hadFocus.GrabFocus();
		}
	}

	private void LoadGarageCar(string? path = null)
	{
		GarageNode.DestroyAllChildren();
		_loadedCar = null;
		if (path != null)
		{
			_loadedCar = GD.Load<PackedScene>(path).Instantiate<Car>();
			GarageNode.AddChild(_loadedCar);
		
			_loadedCar.GlobalTransform = GameManager.Singleton.GetStartPoint();
			_loadedCar.ResetPhysicsInterpolation();

			GarageCameraBase.GlobalTransform = _loadedCar.GlobalTransform;
		}
	}

	public async GDTaskVoid OnSettingsButtonPressed()
	{
		_hadFocus = GetViewport().GuiGetFocusOwner();
		MainMenuContainer.Visible = false;
		
		GameManager.Singleton.SettingsMenu.Show();
		await GDTask.ToSignal(GameManager.Singleton.SettingsMenu, CanvasItem.SignalName.Hidden);

		MainMenuContainer.Visible = true;
		_hadFocus.GrabFocus();
	}

	public void OnExitButtonPressed()
	{
		GetTree().Root.PropagateNotification((int)NotificationWMCloseRequest);
		GetTree().Quit();
	}
	
	public void OnTrackListBackButton()
	{
		TrackListPanel.Hide();
		if (_hadFocus != null)
			_hadFocus.GrabFocus();
	}

	private async GDTaskVoid OpenEditor()
	{
		_hadFocus = GetViewport().GuiGetFocusOwner();
		IsVisible = false;
		LoadGarageCar();

		GameManager.Singleton.NewTrack();

		GameManager.Singleton.Track.Options.AuthorName = GameManager.Singleton.SettingsMenu.GetLocalPlayerName();
		
		Editor.Singleton.IsRunning = true;
		Editor.Singleton.SetupOptions();

		await GDTask.ToSignal(Editor.Singleton, Editor.SignalName.Exited);

		LoadGarageCar(GameManager.CarsPath + GameManager.Singleton.Track.Options.CarType);
		IsVisible = true;
		_hadFocus.GrabFocus();
	}

	private async GDTaskVoid OpenTrack(string path)
	{
		_hadFocus = GetViewport().GuiGetFocusOwner();
		IsVisible = false;
		LoadGarageCar();

		GameManager.Singleton.OpenTrack(path);
		GameManager.Singleton.Play();

		await GDTask.ToSignal(GameManager.Singleton, GameManager.SignalName.StoppedPlaying);
		
		LoadGarageCar(GameManager.CarsPath + GameManager.Singleton.Track.Options.CarType);
		IsVisible = true;
		_hadFocus.GrabFocus();
	}
	
	private void FillTrackContainer(string basePath)
	{
		TrackContainer.DestroyAllChildren();
		var trackList = LoadTrackList(basePath);
		
		foreach (var trackPath in trackList)
		{
			var options = GameManager.Singleton.GetTrackOptions(basePath + trackPath);
			
			if (options == null)
				continue;
			
			if (options.AuthorTime > 0)
			{
				var button = new Button();
				button.CustomMinimumSize = 64 * Vector2.One;
				button.Text = options.Name + "\n" + options.CarType.Split(".")[0].ToUpper();
					
				var loadedPb = GameModeUtils.LoadUserPb(options.Uid);
				if (loadedPb != TimeSpan.Zero)
				{
					button.Text += "\n" + loadedPb.ToString("mm") + ":" + loadedPb.ToString("ss") + "." + loadedPb.ToString("fff");
					button.Text += "\n" + GameModeUtils.GetMedalFromTime((int)loadedPb.TotalMilliseconds, options.AuthorTime);
				}
				
				button.Pressed += () => OpenTrack(basePath + trackPath).Forget();

				button.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
				
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

		var first = TrackContainer.GetChild(0) as Control;
		if (first != null)
			first.GrabFocus();
	}
	private IOrderedEnumerable<string> LoadTrackList(string path)
	{
		return DirAccess.Open(path)
			.GetFiles()
			.Where(file => file.EndsWith(".tk.jz"))
			.ToList().Order();
	}

	public void OnPlayerSetNewName(string newName)
	{
		_loadedCar.SetPlayerName(newName);
		GameManager.Singleton.SettingsMenu.SetLocalPlayerName(newName);
	}

	public void OnCampaignBack()
	{
		CampaignControl.Hide();
		CampaignContainer.DestroyAllChildren();
	}

	private void AddCampaign(string campaignName, string directoryName)
	{
		_campaigns.Add(new Campaign(campaignName, directoryName));
	}
}