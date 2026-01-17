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

	[Export] public Button PlayButton;
	[Export] public Button SettingsButton;
	[Export] public Control SettingsMenu;
	[Export] public Control TrackListPanel;
	[Export] public GridContainer TrackContainer;
	[Export] public TextureRect TrackListImage;
	[Export] public RichTextLabel TrackListLabel;
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
	[Export] public Editor Editor;
	[Export] public RichTextLabel CarDescLabel;
	[Export] public PanelContainer CreditsPanel;
	[Export] public Button FolderButton;
	
	[Export(PropertyHint.FilePath)] public string DefaultCarPath;

	private Car _loadedCar;
	private IOrderedEnumerable<string> _carList;
	private Control _hadFocus;

	private string TrackListSelectedTrackPath = "";
	
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
	
	private List<Campaign> _campaigns = new();
	
	public override void _Ready()
	{
		Editor.IsRunning = false;

		GameManager.Instance.ViewportSettingsChanged += OnViewportSettingsChanged;
		SettingsButton.Pressed += () => OnSettingsButtonPressed().Forget();
		SplitscreenFoldableContainer.Hidden += () => SplitscreenFoldableContainer.Folded = true;
		
		_carList = CarManager.Instance.LoadCarList();
		LoadGarageCar(DefaultCarPath);
		
		AddCampaign("Tutorial", "tutorial");
		AddCampaign("Main Campaign", "main");
		
		PlayButton.CallDeferred("grab_focus");
	}

	public override void _ExitTree()
	{
		GameManager.Instance.ViewportSettingsChanged -= OnViewportSettingsChanged;
	}

	private void OnViewportSettingsChanged()
	{
		GarageViewport.MatchViewport(GameManager.Instance.RootViewport);
	}

	public void OnPlayButtonPressed()
	{
		_hadFocus = GetViewport().GuiGetFocusOwner();
		
		foreach (Campaign campaign in _campaigns)
		{
			var button = new Button();
			button.CustomMinimumSize = 256 * Vector2.One;
			button.Text = campaign.Name;
			button.Pressed += () =>
			{
				FillTrackContainer(CampTracksPath + campaign.DirectoryName + "/");
				TrackListPanel.Show();
			};

			CampaignContainer.AddChild(button);		
		}
	
		CampaignControl.Show();
		
		FolderButton.Visible = false;
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

		FolderButton.Visible = true;
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
				button.Text = GD.Load<PackedScene>(CarManager.CarsPath + car).Instantiate<Car>().CarName;
				button.Pressed += () => LoadGarageCar(CarManager.CarsPath + car);

				GarageContainer.AddChild(button);
			}

			_hadFocus = GetViewport().GuiGetFocusOwner();
			GarageContainer.GetChild<Control>(0).GrabFocus();

			PlayerNameText.Text = SettingsManager.Instance.GetLocalPlayerName();
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
		
			_loadedCar.GlobalTransform = TrackManager.Instance.GetStartPoint();
			_loadedCar.ResetPhysicsInterpolation();

			_loadedCar.InputToggleLights();
			
			GarageCameraBase.GlobalTransform = _loadedCar.GlobalTransform;
			
			CarDescLabel.Text = _loadedCar.CarDescription;
		}
	}

	public async GDTaskVoid OnSettingsButtonPressed()
	{
		_hadFocus = GetViewport().GuiGetFocusOwner();
		MainMenuContainer.Visible = false;
		
		SettingsMenu.Show();
		await GDTask.ToSignal(SettingsMenu, CanvasItem.SignalName.Hidden);

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

		TrackManager.Instance.NewTrack();

		TrackManager.Instance.Track.Options.AuthorName = SettingsManager.Instance.GetLocalPlayerName();
		
		Editor.IsRunning = true;
		Editor.SetupOptions();

		await GDTask.ToSignal(Editor, Editor.SignalName.Exited);

		LoadGarageCar(CarManager.CarsPath + TrackManager.Instance.Track.Options.CarType);
		IsVisible = true;
		_hadFocus.GrabFocus();
	}

	private async GDTaskVoid OpenTrack(string path)
	{
		_hadFocus = GetViewport().GuiGetFocusOwner();
		IsVisible = false;
		LoadGarageCar();

		TrackManager.Instance.OpenTrack(path);
		GameManager.Instance.Play();

		await GDTask.ToSignal(GameManager.Instance, GameManager.SignalName.StoppedPlaying);
		
		LoadGarageCar(CarManager.CarsPath + TrackManager.Instance.Track.Options.CarType);
		IsVisible = true;
		_hadFocus.GrabFocus();
	}
	
	private void FillTrackContainer(string basePath)
	{
		TrackContainer.DestroyAllChildren();
		var trackList = LoadTrackList(basePath);

		bool first = true;
		
		foreach (var trackPath in trackList)
		{
			var options = TrackManager.Instance.GetTrackOptions(basePath + trackPath);
			
			if (options == null)
				continue;
			
			if (options.AuthorTime > 0)
			{
				var button = new Button();
				button.CustomMinimumSize = 64 * Vector2.One;
				button.Text = options.Name;

				button.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
				
				Image image = new Image();
				if (image.LoadJpgFromBuffer(Marshalls.Base64ToRaw(options.PreviewImage)) != Error.Ok)
				{
					image = Image.CreateEmpty(512, 512, true, Image.Format.Rgb8);
				}

				Image icon = new Image();
				icon.CopyFrom(image);
				icon.Resize(128, 128, Image.Interpolation.Cubic);
				button.SetButtonIcon(ImageTexture.CreateFromImage(icon));
				
				button.Pressed += () => TrackListSelectTrack(basePath, trackPath, options, image);
				
				TrackContainer.AddChild(button);
				
				if (first)
				{
					first = false;
					TrackListSelectTrack(basePath, trackPath, options, image);
					button.GrabFocus();
				}
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

	private void TrackListSelectTrack(string basePath, string trackPath, TrackOptions options, Image image)
	{
		TrackListLabel.Text = options.Name + "\n" + GD.Load<PackedScene>(CarManager.CarsPath + options.CarType).Instantiate<Car>().CarName;
		
		image.Resize(320, 320, Image.Interpolation.Lanczos);
		TrackListImage.SetTexture(ImageTexture.CreateFromImage(image));
		
		var loadedPb = GameModeUtils.LoadUserPb(options.Uid);
		if (loadedPb != TimeSpan.Zero)
		{
			TrackListLabel.Text += "\n" + loadedPb.ToString("mm") + ":" + loadedPb.ToString("ss") + "." + loadedPb.ToString("fff");
			TrackListLabel.Text += "\n" + GameModeUtils.GetMedalFromTime((int)loadedPb.TotalMilliseconds, options.AuthorTime);
		}
		
		TrackListSelectedTrackPath = basePath + trackPath;
	}

	public void TrackListOnPlayTrackButtonPressed()
	{
		OpenTrack(TrackListSelectedTrackPath).Forget();
	}

	public void OnPlayerSetNewName(string newName)
	{
		_loadedCar.SetPlayerName(newName);
		SettingsManager.Instance.SetLocalPlayerName(newName);
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

	public void OnCredits()
	{
		MainMenuContainer.Visible = false;
		CreditsPanel.Show();
	}

	public void OnExitCredits()
	{
		MainMenuContainer.Visible = true;
		CreditsPanel.Hide();
	}

	public void OnFolderButton()
	{
		OS.ShellOpen(ProjectSettings.GlobalizePath(UserTracksPath));
	}
}