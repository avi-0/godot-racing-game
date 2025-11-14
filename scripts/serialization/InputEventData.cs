using Godot;

namespace racingGame;

public abstract class InputEventData
{
	public abstract InputEvent Load();

	public static InputEventData Save(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent)
			return InputEventKeyData.Save(keyEvent);
		if (@event is InputEventJoypadButton joypadButtonEvent)
			return InputEventJoypadButtonData.Save(joypadButtonEvent);
		if (@event is InputEventJoypadMotion joypadMotionEvent)
			return InputEventJoypadMotionData.Save(joypadMotionEvent);

		return null;
	}
}