using Fractural.Tasks;
using Godot;

namespace racingGame;

public partial class PauseMenu : Control
{
	[Export] public Button ResumeButton;
	[Export] public Button SettingsButton;
	[Export] public Button ExitButton;
	
	public override void _Ready()
	{
		ResumeButton.Pressed += OnResumeButton;
		SettingsButton.Pressed += () => OnSettingsButton().Forget();
		ExitButton.Pressed += OnExitButton;
		VisibilityChanged += OnVisibilityChanged;
	}

	public void OnResumeButton()
	{
		Hide();
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public async GDTaskVoid OnSettingsButton()
	{
		GameManager.Singleton.SettingsMenu.Show();
		
		await GDTask.ToSignal(GameManager.Singleton.SettingsMenu, CanvasItem.SignalName.Hidden);
		
		SettingsButton.GrabFocus();
	}

	public void OnExitButton()
	{
		Hide();
		GameManager.Singleton.Stop();
	}

	public void OnVisibilityChanged()
	{
		if (Visible)
			ResumeButton.GrabFocus();
	}
}