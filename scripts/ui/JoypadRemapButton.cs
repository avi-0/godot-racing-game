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
			.SelectMany(FormatMapping));
	}

	private string[] FormatMapping(InputEvent @event)
	{
		if (@event is InputEventJoypadMotion joypadMotionEvent)
		{
			string text = JoyPadMotionToString(joypadMotionEvent);

			text += joypadMotionEvent.AxisValue > 0 ? "+" : "-";
					
			return [text];
		}
				
		if (@event is InputEventJoypadButton joypadButtonEvent)
		{
			string text = JoyPadButtonToString(joypadButtonEvent);

			return [text];
		}

		return [];
	}

	private string JoyPadMotionToString(InputEventJoypadMotion joypadMotionEvent)
	{
		return joypadMotionEvent.Axis switch
		{
		JoyAxis.LeftX => "l_stick",
		JoyAxis.LeftY => "r_stick",
		JoyAxis.RightX => "l_stick",
		JoyAxis.RightY => "r_stick",
		JoyAxis.TriggerLeft => "lt",
		JoyAxis.TriggerRight => "rt",
		_ => "[UNKNOWN]"
		};
	}

	private string JoyPadButtonToString(InputEventJoypadButton joypadButtonEvent)
	{
		return joypadButtonEvent.ButtonIndex switch
		{
			JoyButton.A => "a",
			JoyButton.B => "b",
			JoyButton.X => "x",
			JoyButton.Y => "y",
			JoyButton.DpadDown => "dpad_down",
			JoyButton.DpadUp => "dpad_up",
			JoyButton.DpadLeft => "dpad_left",
			JoyButton.DpadRight => "dpad_right",
			JoyButton.Back => "select",
			JoyButton.Start => "start",
			JoyButton.Guide => "Home",
			JoyButton.LeftShoulder => "LB",
			JoyButton.RightShoulder => "RB",
			JoyButton.LeftStick => "l_stick_click",
			JoyButton.RightStick => "r_stick_click",
			_ => "[UNKNOWN]"
		};
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

	protected override bool LoadInputTextures()
	{
		foreach (InputEvent @event in InputMap.ActionGetEvents(Action))
		{
			string path = "";
			if (@event is InputEventJoypadButton inputEventJoypadButton)
			{
				path = JoyPadButtonToString(inputEventJoypadButton);
			}
			else if (@event is InputEventJoypadMotion inputEventJoypadMotion)
			{
				path = JoyPadMotionToString(inputEventJoypadMotion);
			}
			
			if (path != "" && path != "[UNKNOWN]")
			{
				ControllerIconTexture controllerIconTexture = new ControllerIconTexture();
				controllerIconTexture.path = "joypad/" + path;
				controllerIconTexture.force_type = ControllerIcons.EInputType.CONTROLLER;
				SetButtonIcon(controllerIconTexture);
				SetIconAlignment(HorizontalAlignment.Center);
				SetExpandIcon(true);
			}
		}
		return true;
	}
}