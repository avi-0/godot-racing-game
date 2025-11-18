using Godot;

namespace racingGame;

public class InputEventJoypadButtonData : InputEventData
{
	public JoyButton ButtonIndex;
	
	public override InputEvent Load()
	{
		var joypadButtonEvent = new InputEventJoypadButton();
		joypadButtonEvent.ButtonIndex = ButtonIndex;

		joypadButtonEvent.Device = -1;

		return joypadButtonEvent;
	}

	public static InputEventJoypadButtonData Save(InputEventJoypadButton joypadButtonEvent)
	{
		var data = new InputEventJoypadButtonData();

		data.ButtonIndex = joypadButtonEvent.ButtonIndex;

		return data;
	}
}