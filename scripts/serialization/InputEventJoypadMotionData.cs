using Godot;

namespace racingGame;

public class InputEventJoypadMotionData : InputEventData
{
	public JoyAxis Axis;
	public int Value;
	
	public override InputEvent Load()
	{
		var joypadMotionEvent = new InputEventJoypadMotion();
		joypadMotionEvent.Axis = Axis;
		joypadMotionEvent.AxisValue = Value;

		return joypadMotionEvent;
	}

	public static InputEventJoypadMotionData Save(InputEventJoypadMotion joypadMotionEvent)
	{
		var data = new InputEventJoypadMotionData();

		data.Axis = joypadMotionEvent.Axis;
		data.Value = (int) float.Round(joypadMotionEvent.AxisValue);

		return data;
	}
}