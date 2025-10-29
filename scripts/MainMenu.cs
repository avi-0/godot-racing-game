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
	
	public void _on_play_button_pressed()
	{
		OpenTrack("res://tracks/TestTrack.tk.tscn");
	}

	public async void _on_editor_button_pressed()
	{
		OpenEditor().Forget();
	}

	public void _on_load_button_pressed()
	{
		MenuFileDialog.Show();
	}

	public void _on_settings_button_pressed()
	{

	}

	public void _on_exit_button_pressed()
	{
		GetTree().Root.PropagateNotification((int)NotificationWMCloseRequest);
		GetTree().Quit();
	}

	public void _on_menu_file_dialog_file_selected(string path)
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
