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

	public override void _Ready()
	{
		Editor.Singleton.IsRunning = false;
	}

	public override void _Process(double delta)
	{
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
			if (options == null)
				continue;
			
			if (options.AuthorTime > 0)
			{
				var button = new Button();
				button.CustomMinimumSize = 64 * Vector2.One;
				button.Text = options.Name;
				button.Pressed += () => OpenTrack(basePath + trackPath).Forget();

				TrackContainer.AddChild(button);
			}
		}
	}
	private IOrderedEnumerable<string> LoadTrackList(string path)
	{
		return ResourceLoader.ListDirectory(path)
			.Where(filePath => filePath.EndsWith(".tk.json"))
			.ToList().Order();
	}
}