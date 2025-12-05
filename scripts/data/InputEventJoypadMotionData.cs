using Godot;

namespace racingGame.data;

public class InputEventJoypadMotionData : InputEventData
{
	public JoyAxis Axis;
	public int Value;
	
	public override InputEvent Load()
	{
		var joypadMotionEvent = new InputEventJoypadMotion();
		joypadMotionEvent.Axis = Axis;
		joypadMotionEvent.AxisValue = Value;

		joypadMotionEvent.Device = -1;

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