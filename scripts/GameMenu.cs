using Godot;
using System;

public partial class GameMenu : Control
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void OnResumeButton()
	{
		this.Hide();
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public void OnSettingsButton()
	{
		GameManager.Singleton.SettingsMenu.Show();
	}

	public void OnExitButton()
	{
		this.Hide();
		GameManager.Singleton.Stop();
	}	
}
