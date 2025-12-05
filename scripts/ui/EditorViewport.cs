using Godot;

namespace racingGame;

public partial class EditorViewport : SubViewport
{
	[Signal]
	public delegate void InputEventHandler(InputEvent @event);

	public override void _UnhandledInput(InputEvent @event)
	{
		EmitSignalInput(@event);
	}
}