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
	private Control _hadFocus;
	
	public string MultiplayerSelectedTrackPath = "";
	
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
				TrackListPanel.FillTrackContainer(CampTracksPath + campaign.DirectoryName + "/", true, campaign.Name, path => { OpenTrack(path);});
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
		
		TrackListPanel.FillTrackContainer(UserTracksPath, false, "Local Tracks", path => { OpenTrack(path);});
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

	public void OnMultiplayerButton()
	{
		MultiplayerWindow.Show();
	}

	public void OnMultiplayerBack()
	{
		MultiplayerWindow.Hide();
	}

	public void OnHostSelectTrackButton()
	{
		TrackListPanel.FillTrackContainer(CampTracksPath + _campaigns[1].DirectoryName + "/" , true, _campaigns[1].Name, HostSelectedTrack);
		TrackListPanel.FillTrackContainer(CampTracksPath + _campaigns[0].DirectoryName + "/" , true, _campaigns[0].Name, HostSelectedTrack, false);
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
}
