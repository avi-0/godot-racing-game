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
	public static MainMenu Instance;
	
	public string CampTracksPath = "res://tracks/";
	public string UserTracksPath = "user://tracks/";

	[Export] public Button PlayButton;
	[Export] public Button SettingsButton;
	[Export] public Control SettingsMenu;
	[Export] public TrackList TrackListPanel;
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
	[Export] public OptionButton SkinButton;
	[Export] public Control MultiplayerWindow;
	[Export] public TextureRect MultiplayerTrackImage;
	[Export] public Label MultiplayerTrackLabel;
	[Export] public LineEdit IPLine;
	
	[Export(PropertyHint.FilePath)] public string DefaultCarPath;

	private Car _loadedCar;
	private IOrderedEnumerable<string> _carList;
	
	public string MultiplayerSelectedTrackPath = "";

	public Control LastPanel;
	public Control HadFocus;
	
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
		Instance = this;
		
		Editor.IsRunning = false;

		GameManager.Instance.ViewportSettingsChanged += OnViewportSettingsChanged;
		SettingsButton.Pressed += () => OnSettingsButtonPressed().Forget();
		SplitscreenFoldableContainer.Hidden += () => SplitscreenFoldableContainer.Folded = true;
		
		_carList = CarManager.Instance.LoadCarList();
		LoadGarageCar(DefaultCarPath);
		
		AddCampaign("Tutorial", "tutorial");
		AddCampaign("Main Campaign", "main");
		
		PlayButton.CallDeferred("grab_focus");
		
		SkinButton.ItemSelected += (index) =>
		{
			_loadedCar.SetSkin((int)index);
			SettingsManager.Instance.Settings.SelectedSkins[_loadedCar.CarName] = (int)index;
			SettingsManager.Instance.SaveSettings();
		};

		MainMenuContainer.VisibilityChanged += OnBackToMenu;
		VisibilityChanged += OnBackToMenu;
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
		MainMenuContainer.Visible = false;
		HadFocus = GetViewport().GuiGetFocusOwner();

		bool first = true;
		foreach (Campaign campaign in _campaigns)
		{
			var button = new Button();
			button.CustomMinimumSize = 256 * Vector2.One;
			button.Text = campaign.Name;
			button.Pressed += () =>
			{
				HadFocus = GetViewport().GuiGetFocusOwner();
				CampaignControl.Hide();
				LastPanel = CampaignControl;
				TrackListPanel.FillTrackContainer(CampTracksPath + campaign.DirectoryName + "/", true, campaign.Name, GameModeUtils.GAMEMODE_TIMEATTACK, path => { OpenTrack(path);});
			};

			CampaignContainer.AddChild(button);

			if (first)
			{
				first = false;
				button.GrabFocus();
			}
		}
		
		CampaignControl.Show();
		MainMenuContainer.Hide();
	}

	public void OnEditorButtonPressed()
	{
		OpenEditor().Forget();
	}

	public void OnLoadButtonPressed()
	{
		HadFocus = GetViewport().GuiGetFocusOwner();

		MainMenuContainer.Visible = false;
		LastPanel = MainMenuContainer;
		TrackListPanel.FillTrackContainer(UserTracksPath, false, "Local Tracks", GameModeUtils.GAMEMODE_TIMEATTACK, path => { OpenTrack(path);});
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

			HadFocus = GetViewport().GuiGetFocusOwner();
			GarageContainer.GetChild<Control>(0).GrabFocus();

			PlayerNameText.Text = SettingsManager.Instance.GetLocalPlayerName();
		}
		else
		{
			MainMenuContainer.Visible = true;
			
			GarageContainer.DestroyAllChildren();
			
			if (HadFocus != null)
				HadFocus.GrabFocus();
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
		

			Transform3D spawn = TrackManager.Instance.GetStartPoint();
			spawn.Origin = new Vector3(spawn.Origin.X, spawn.Origin.Y + _loadedCar.FrontWheelConfig.SpringRest + 0.1f, spawn.Origin.Z);
			_loadedCar.GlobalTransform = spawn;
			_loadedCar.ResetPhysicsInterpolation();

			_loadedCar.InputToggleLights();
			
			GarageCameraBase.GlobalTransform = _loadedCar.GlobalTransform;
			
			CarDescLabel.Text = _loadedCar.CarDescription;

			if (_loadedCar.Skins != null && _loadedCar.Skins.Length > 0)
			{
				SkinButton.Clear();

				for (int skin = 0; skin < _loadedCar.Skins.Length; skin++)
				{
					SkinButton.AddItem(skin.ToString(), skin);
				}
				
				SkinButton.Show();
				
				if (SettingsManager.Instance.Settings.SelectedSkins.ContainsKey(_loadedCar.CarName) && SettingsManager.Instance.Settings.SelectedSkins[_loadedCar.CarName] > 0 && _loadedCar.Skins[SettingsManager.Instance.Settings.SelectedSkins[_loadedCar.CarName]] != null)
				{
					_loadedCar.SetSkin(SettingsManager.Instance.Settings.SelectedSkins[_loadedCar.CarName]);
					SkinButton.Selected = SettingsManager.Instance.Settings.SelectedSkins[_loadedCar.CarName];
				}
				else
				{
					_loadedCar.SetSkin(0);
				}
			}
			else
			{
				SkinButton.Hide();
			}
		}
	}

	public async GDTaskVoid OnSettingsButtonPressed()
	{
		HadFocus = GetViewport().GuiGetFocusOwner();
		MainMenuContainer.Visible = false;
		
		SettingsMenu.Show();
		await GDTask.ToSignal(SettingsMenu, CanvasItem.SignalName.Hidden);

		MainMenuContainer.Visible = true;
		HadFocus.GrabFocus();
	}

	public void OnExitButtonPressed()
	{
		GetTree().Root.PropagateNotification((int)NotificationWMCloseRequest);
		GetTree().Quit();
	}

	private async GDTaskVoid OpenEditor()
	{
		HadFocus = GetViewport().GuiGetFocusOwner();
		IsVisible = false;
		LoadGarageCar();

		TrackManager.Instance.NewTrack();

		TrackManager.Instance.Track.Options.AuthorName = SettingsManager.Instance.GetLocalPlayerName();
		
		Editor.IsRunning = true;
		Editor.SetupOptions();

		await GDTask.ToSignal(Editor, Editor.SignalName.Exited);

		LoadGarageCar(CarManager.CarsPath + TrackManager.Instance.Track.Options.CarType);
		IsVisible = true;
		HadFocus.GrabFocus();
	}

	public async GDTaskVoid OpenTrack(string path, bool host = true)
	{
		IsVisible = false;
		LoadGarageCar();

		TrackManager.Instance.OpenTrack(path);
		GameManager.Instance.Play(host);

		await GDTask.ToSignal(GameManager.Instance, GameManager.SignalName.StoppedPlaying);
		
		LoadGarageCar(CarManager.CarsPath + TrackManager.Instance.Track.Options.CarType);
		IsVisible = true;
	}

	public void OnPlayerSetNewName(string newName)
	{
		_loadedCar.SetPlayerName(newName);
		SettingsManager.Instance.SetLocalPlayerName(newName);
	}

	public void OnCampaignBack()
	{
		MainMenuContainer.Visible = true;
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
		((Control)CreditsPanel.FindChild("BackButton")).GrabFocus();
		CreditsPanel.Show();
	}

	public void OnExitCredits()
	{
		MainMenuContainer.Visible = true;
		CreditsPanel.Hide();
	}

	public void OnMultiplayerButton()
	{
		MainMenuContainer.Visible = false;
		MultiplayerWindow.Show();
	}

	public void OnMultiplayerBack()
	{
		MainMenuContainer.Visible = true;
		MultiplayerWindow.Hide();
	}

	public void OnHostSelectTrackButton()
	{
		MultiplayerWindow.Visible = false;
		LastPanel = MultiplayerWindow;
		
		TrackListPanel.FillTrackContainer(CampTracksPath + _campaigns[1].DirectoryName + "/" , true, _campaigns[1].Name, GameModeUtils.GAMEMODE_TIMEATTACK, HostSelectedTrack);
		TrackListPanel.FillTrackContainer("res://tracks/mp/" , false, "Multiplayer Tracks", GameModeUtils.GAMEMODE_TIMEATTACK, HostSelectedTrack, false);
	}

	public void HostSelectedTrack(string path)
	{
		var options = TrackManager.Instance.GetTrackOptions(path);
		
		MultiplayerTrackLabel.Text = options.Name + "\n" + GD.Load<PackedScene>(CarManager.CarsPath + options.CarType).Instantiate<Car>().CarName;
		
		Image image = TrackManager.Instance.GetTrackImage(options);
		image.Resize(160, 160, Image.Interpolation.Lanczos);
		MultiplayerTrackImage.SetTexture(ImageTexture.CreateFromImage(image));
		
		MultiplayerSelectedTrackPath = path;
	}

	public void OnHostServerButton()
	{
		if (MultiplayerSelectedTrackPath != "")
		{
			OpenTrack(MultiplayerSelectedTrackPath).Forget();
			MultiplayerManager.Instance.CreateServer(MultiplayerSelectedTrackPath);
		}
	}

	public async void OnConnectToServerButton()
	{
		MultiplayerManager.Instance.CreateClient(IPLine.Text);

		await GDTask.ToSignal(MultiplayerManager.Instance, MultiplayerManager.SignalName.ConnectionAttemptEnded);
		
		if (MultiplayerManager.Instance.LastConnectionAttemptStatus == MultiplayerManager.CONNECTION_STATUS_CONNECTED && MultiplayerManager.Instance.ServerInfo.TrackPath != "")
		{
			OpenTrack(MultiplayerManager.Instance.ServerInfo.TrackPath, false).Forget();
		}
	}

	public void OnBackToMenu()
	{
		if (HadFocus != null)
		{
			HadFocus.GrabFocus();
		}
		if (MainMenuContainer.Visible)
		{
			((Control)MainMenuContainer.FindChild("PlayButton")).GrabFocus();
		}
	}
}
