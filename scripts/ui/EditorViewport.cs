using Godot;

namespace racingGame;

public partial class EditorViewport : SubViewport
{
	[Signal]
	public delegate void InputEventHandler(InputEvent @event);

	public override void _Ready()
	{
		GameManager.Instance.ViewportSettingsChanged += OnViewportSettingsChanged;
	}

	public override void _ExitTree()
	{
		GameManager.Instance.ViewportSettingsChanged -= OnViewportSettingsChanged;
	}

	private void OnViewportSettingsChanged()
	{
		this.MatchViewport(GameManager.Instance.RootViewport);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		EmitSignalInput(@event);
	}
}