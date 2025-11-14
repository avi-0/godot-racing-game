using Godot;
using System;
using System.Linq;

public partial class KeyboardRemapButton : Button
{
	[Export] public StringName Action;

	private bool _isRemapping = false;
	
	public override void _Ready()
	{
		LoadFromInputMap();
	}

	private void OnPressed(MouseButton mouseButton)
	{
		if (mouseButton == MouseButton.Right)
		{
			InputMap.ActionEraseEvents(Action);
			LoadFromInputMap();
			
			_isRemapping = false;

			return;
		}
		
		if (!_isRemapping)
		{
			_isRemapping = true;

			Text = "Press key...";
		}
		else
		{
			_isRemapping = false;
			LoadFromInputMap();
		}
	}

	private void LoadFromInputMap()
	{
		Text = String.Join(", ", InputMap
			.ActionGetEvents(Action)
			.Where(@event => @event is InputEventKey)
			.Cast<InputEventKey>()
			.Select(keyEvent => DisplayServer.KeyboardGetKeycodeFromPhysical(keyEvent.PhysicalKeycode)));
		
		// prevents button from collapsing due to zero lines of text
		if (Text == "")
			Text = " ";
	}

	public override void _Input(InputEvent @event)
	{
		if (_isRemapping)
		{
			if (@event is InputEventKey keyEvent)
			{
				var settingEvent = new InputEventKey();
				settingEvent.PhysicalKeycode = keyEvent.PhysicalKeycode;
				
				InputMap.ActionAddEvent(Action, settingEvent);
				
				LoadFromInputMap();

				_isRemapping = false;
			}
		}
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseButtonEvent && mouseButtonEvent.IsPressed())
		{
			OnPressed(mouseButtonEvent.ButtonIndex);
		}
	}
}
