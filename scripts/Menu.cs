using Godot;
using System;
using racingGame;
using Fractural.Tasks;

public partial class Menu : Control
{

	[Export] public FileDialog MenuFileDialog;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (InEditor && Input.IsKeyPressed(Key.Escape) && !GameInstance.GetNode<GameManager>("GameManager").IsPlaying())
		{
			InEditor = false;
			UnloadGame();
		}
	}

	private bool InEditor = false;

	private Node GameInstance;

	//--USER INPUTS
	public void _on_play_button_pressed()
	{
		OpenTrack("res://tracks/TestTrack.tk.tscn");
	}

	public async void _on_editor_button_pressed()
	{
		LoadGame();
		InEditor = true;
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
		OpenTrack(path);
	}
	//--

	private void LoadGame()
	{
		GameInstance = GD.Load<PackedScene>("res://scenes/game.tscn").Instantiate();
		GetTree().Root.AddChild(GameInstance);

		Visible = false;
	}

	private void UnloadGame()
	{
		GameInstance.QueueFree();
		GetTree().Root.RemoveChild(GameInstance);

		Visible = true;
	}

	private async void OpenTrack(string path)
	{
		LoadGame();

		var GameManager = GameInstance.GetNode<GameManager>("GameManager");
		GameManager.Singleton.OpenTrack(path);
		GameManager.Singleton.Play();

		var EditorUI = GameInstance.GetNode<Control>("Editor/EditorUI");
		EditorUI.Visible = false;

		await GDTask.ToSignal(GameManager.Singleton, GameManager.SignalName.StoppedPlaying);
		UnloadGame();
	}
}
