using System;
using System.Linq;
using Godot;
using Godot.Collections;

namespace racingGame;

public partial class JoypadRemapButton : RemapButton
{
	protected override string FormatMappings(Array<InputEvent> events)
	{
		return String.Join(", ", events
			.SelectMany<InputEvent, string>(@event =>
			{
				if (@event is InputEventJoypadMotion joypadMotionEvent)
				{
					string text = joypadMotionEvent.Axis switch
					{
						JoyAxis.LeftX => "LeftX",
						JoyAxis.LeftY => "LeftY",
						JoyAxis.RightX => "RightX",
						JoyAxis.RightY => "RightY",
						JoyAxis.TriggerLeft => "LT",
						JoyAxis.TriggerRight => "RT",
						_ => "[UNKNOWN]"
					};

					text += joypadMotionEvent.AxisValue > 0 ? "+" : "-";
					
					return [text];
				}
				
				if (@event is InputEventJoypadButton joypadButtonEvent)
				{
					string text = joypadButtonEvent.ButtonIndex switch
					{
						JoyButton.A => "A",
						JoyButton.B => "B",
						JoyButton.X => "X",
						JoyButton.Y => "Y",
						JoyButton.DpadDown => "Down",
						JoyButton.DpadUp => "Up",
						JoyButton.DpadLeft => "Left",
						JoyButton.DpadRight => "Right",
						JoyButton.Back => "Select",
						JoyButton.Start => "Start",
						JoyButton.Guide => "Home",
						JoyButton.LeftShoulder => "LB",
						JoyButton.RightShoulder => "RB",
						JoyButton.LeftStick => "LS",
						JoyButton.RightStick => "RS",
						_ => "[UNKNOWN]"
					};

					return [text];
				}

				return [];
			}));
	}

	protected override bool TryRemapEvent(InputEvent @event)
	{
		if (@event is InputEventJoypadMotion joypadMotionEvent && float.Abs(joypadMotionEvent.AxisValue) > 0.8)
		{
			var settingEvent = new InputEventJoypadMotion();
			settingEvent.Device = (int) InputEvent.DeviceIdEmulation;
			settingEvent.Axis = joypadMotionEvent.Axis;
			settingEvent.AxisValue = float.Sign(joypadMotionEvent.AxisValue);
			
			EraseMappings();
			InputMap.ActionAddEvent(Action, settingEvent);

			return true;
		}

		if (@event is InputEventJoypadButton joypadButtonEvent && joypadButtonEvent.Pressed)
		{
			var settingEvent = new InputEventJoypadButton();
			settingEvent.Device = (int) InputEvent.DeviceIdEmulation;
			settingEvent.ButtonIndex = joypadButtonEvent.ButtonIndex;
			settingEvent.Pressed = true;
			
			EraseMappings();
			InputMap.ActionAddEvent(Action, settingEvent);

			return true;
		}
		
		return false;
	}

	protected override void EraseMappings()
	{
		foreach (var @event in InputMap.ActionGetEvents(Action))
		{
			if (@event is InputEventJoypadButton || @event is InputEventJoypadMotion)
				InputMap.ActionEraseEvent(Action, @event);
		}
	}

	protected override string GetRemappingPrompt()
		=> "Press key...";
}