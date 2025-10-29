using Godot;
using System;
using racingGame;
using System.Linq;
using Fractural.Tasks;

public partial class MainMenu : Control
{
	[Export] public FileDialog MenuFileDialog;
	[Export] public Control CampMenu;
	[Export] public GridContainer TrackContainer;

	public string CampTracksPath = "res://tracks/";

	public override void _Ready()
	{
		Editor.Singleton.IsRunning = false;
	
		var trackList = LoadCampTracksList();

		foreach (var trackPath in trackList)
		{
			var button = new Button();
			button.CustomMinimumSize = 64 * Vector2.One;
			button.Text = trackPath;
			button.Pressed += () => OpenTrack(CampTracksPath+trackPath);

			TrackContainer.AddChild(button);
		}
	}
	
	public override void _Process(double delta)
	{
	}
	
	public void OnPlayButtonPressed()
	{
		CampMenu.Show();
	}

	public async void OnEditorButtonPressed()
	{
		OpenEditor().Forget();
	}

	public void OnLoadButtonPressed()
	{
		MenuFileDialog.Show();
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

	public void OnMenuFileDialogFileSelected(string path)
	{
		MenuFileDialog.Hide();
		OpenTrack(path).Forget();
	}

	public void OnCampBackButton()
	{
		CampMenu.Hide();
	}

	private async GDTaskVoid OpenEditor()
	{
		Visible = false;

		GameManager.Singleton.NewTrack();
		Editor.Singleton.IsRunning = true;

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

	private IOrderedEnumerable<string> LoadCampTracksList()
	{
		return ResourceLoader.ListDirectory(CampTracksPath).ToList().Order();
	}
}
