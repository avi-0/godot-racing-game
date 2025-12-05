using Godot;

namespace racingGame.data;

public class InputEventKeyData : InputEventData
{
	public Key PhysicalKeycode;
	
	public override InputEvent Load()
	{
		var keyEvent = new InputEventKey();
		keyEvent.PhysicalKeycode = PhysicalKeycode;

		return keyEvent;
	}

	public static InputEventKeyData Save(InputEventKey keyEvent)
	{
		var data = new InputEventKeyData();

		data.PhysicalKeycode = keyEvent.PhysicalKeycode;

		return data;
	}
}