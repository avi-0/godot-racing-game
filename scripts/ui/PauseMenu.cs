using Fractural.Tasks;
using Godot;

namespace racingGame;

public partial class PauseMenu : Control
{
	[Export] public Button ResumeButton;
	[Export] public Button SettingsButton;
	[Export] public Button ExitButton;
	[Export] public SettingsMenu SettingsMenu;
	
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
		SettingsMenu.Show();
		
		await GDTask.ToSignal(SettingsMenu, CanvasItem.SignalName.Hidden);
		
		SettingsButton.GrabFocus();
	}

	public void OnExitButton()
	{
		Hide();
		GameManager.Instance.Stop();
	}

	public void OnVisibilityChanged()
	{
		if (Visible)
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
			
			ResumeButton.GrabFocus();
		}
		else
		{
			Input.MouseMode = Input.MouseModeEnum.Hidden;
		}
	}
}