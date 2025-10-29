using Godot;
using System;
using racingGame;
using Fractural.Tasks;

public partial class MainMenu : Control
{
	[Export] public FileDialog MenuFileDialog;
	
	public override void _Ready()
	{
		Editor.Singleton.IsRunning = false;
	}
	
	public override void _Process(double delta)
	{
	}
	
	public void OnPlayButtonPressed()
	{
		OpenTrack("res://tracks/TestTrack.tk.tscn");
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
}
