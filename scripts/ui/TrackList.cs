using Godot;
using System;
using System.Linq;
using racingGame;
using racingGame.data;
using racingGame.extensions;

public partial class TrackList : Control
{
	public TrackList Instance;
	
	[Export] public TabContainer TrackContainer;
	[Export] public TextureRect TrackListImage;
	[Export] public RichTextLabel TrackListLabel;
	[Export] public Button FolderButton;
	
	public string SelectedTrackPath = "";

	public Action<string> Callback;
	
	public override void _Ready()
	{
		Instance = this;
	}
	
	public override void _Process(double delta)
	{
	}
	
	public void FillTrackContainer(string basePath, bool isCampaign, string name, int gamemodeType, Action<string> callback, bool emptyPrevious = true)
	{
		Callback = callback;
		
		if (emptyPrevious)
		{
			TrackContainer.DestroyAllChildren();
		}
		
		ScrollContainer scrollContainer = new ScrollContainer();
		scrollContainer.SetName(name);
		scrollContainer.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
		GridContainer gridContainer = new GridContainer();
		gridContainer.Columns = 3;
		scrollContainer.AddChild(gridContainer);
		TrackContainer.AddChild(scrollContainer);
		
		var trackList = LoadTrackList(basePath);
		
		int trackID = 0;
		int silverMedals = 0;
		int goldMedals = 0;
		
		foreach (var trackPath in trackList)
		{
			var options = TrackManager.Instance.GetTrackOptions(basePath + trackPath);
			
			if (options == null)
				continue;
			
			if (!GameModeUtils.GameModeSupportsTrackType(gamemodeType, options.Type))
				continue;
			
			if (options.AuthorTime > 0)
			{
				trackID++;
				
				var button = new Button();
				button.CustomMinimumSize = new Vector2(420, 64);
				button.Text = options.Name;

				button.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
				
				Image icon = TrackManager.Instance.GetTrackImage(options);
				icon.Resize(128, 128, Image.Interpolation.Cubic);
				button.SetButtonIcon(ImageTexture.CreateFromImage(icon));

				bool canPlay = true;
				
				if (isCampaign)
				{
					var loadedPb = GameModeUtils.LoadUserPb(options.Uid);
					if (loadedPb != TimeSpan.Zero)
					{
						if (loadedPb.TotalMilliseconds < GameModeUtils.GetGoldFromAt(options.AuthorTime))
						{
							goldMedals++;
							silverMedals++;
						}
						else if (loadedPb.TotalMilliseconds < GameModeUtils.GetSilverFromAt(options.AuthorTime))
						{
							silverMedals++;
						}
					}
					
					if (trackID > 5 && trackID == trackList.Count())
					{
						if (goldMedals < trackList.Count() - 1)
						{
							canPlay = false;
							Image lockImage = ResourceLoader.Load<CompressedTexture2D>("res://assets/img/gold_lock.png").GetImage();
							lockImage.Resize(128, 128, Image.Interpolation.Cubic);
							button.SetButtonIcon(ImageTexture.CreateFromImage(lockImage));
						}
					}
					else if (trackID > 3 )
					{
						if (silverMedals < trackID / 2)
						{
							canPlay = false;
							Image lockImage = ResourceLoader.Load<CompressedTexture2D>("res://assets/img/silver_lock.png").GetImage();
							lockImage.Resize(128, 128, Image.Interpolation.Cubic);
							button.SetButtonIcon(ImageTexture.CreateFromImage(lockImage));
						}
					}
				}

				if (canPlay)
				{
					button.Pressed += () => TrackListSelectTrack(basePath, trackPath, options); 
				}
				
				gridContainer.AddChild(button);
				
				if (trackID == 1)
				{
					TrackListSelectTrack(basePath, trackPath, options);
					button.GrabFocus();
				}
			}
		}
		
		Show();
	}
	private IOrderedEnumerable<string> LoadTrackList(string path)
	{
		return DirAccess.Open(path)
			.GetFiles()
			.Where(file => file.EndsWith(".tk.jz"))
			.ToList().Order();
	}

	private void TrackListSelectTrack(string basePath, string trackPath, TrackOptions options)
	{
		if (SelectedTrackPath == basePath + trackPath)
		{
			TrackListOnPlayTrackButtonPressed();
			return;
		}
		
		TrackListLabel.Text = options.Name + "\n" + GD.Load<PackedScene>(CarManager.CarsPath + options.CarType).Instantiate<Car>().CarName;

		Image image = TrackManager.Instance.GetTrackImage(options);
		image.Resize(320, 320, Image.Interpolation.Lanczos);
		TrackListImage.SetTexture(ImageTexture.CreateFromImage(image));
		
		var loadedPb = GameModeUtils.LoadUserPb(options.Uid);
		if (loadedPb != TimeSpan.Zero)
		{
			TrackListLabel.Text += "\n" + loadedPb.ToString("mm") + ":" + loadedPb.ToString("ss") + "." + loadedPb.ToString("fff");
			TrackListLabel.Text += "\n" + GameModeUtils.GetMedalFromTime((int)loadedPb.TotalMilliseconds, options.AuthorTime);
		}
		
		SelectedTrackPath = basePath + trackPath;
	}

	public void TrackListOnPlayTrackButtonPressed()
	{
		Callback(SelectedTrackPath);
		OnTrackListBackButton();
	}
	
	public void OnFolderButton()
	{
		OS.ShellOpen(ProjectSettings.GlobalizePath(MainMenu.Instance.UserTracksPath));
	}
	
	public void OnTrackListBackButton()
	{
		Visible = false;
		FolderButton.Visible = false;
		SelectedTrackPath = "";
		
		if (MainMenu.Instance.LastPanel != null)
		{
			MainMenu.Instance.LastPanel.Visible = true;
		}
		if (MainMenu.Instance.HadFocus != null)
		{
			MainMenu.Instance.HadFocus.GrabFocus();
		}
		
		Hide();
	}
}
