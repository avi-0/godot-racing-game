using System;
using System.Linq;
using Godot;
using Godot.Collections;

namespace racingGame;

public partial class KeyboardRemapButton : RemapButton
{
	protected override string FormatMappings(Array<InputEvent> events)
	{
		return String.Join(", ", events
			.Where(@event => @event is InputEventKey)
			.Cast<InputEventKey>()
			.Select(keyEvent => DisplayServer.KeyboardGetKeycodeFromPhysical(keyEvent.PhysicalKeycode)));
	}

	protected override bool TryRemapEvent(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent)
		{
			var settingEvent = new InputEventKey();
			settingEvent.PhysicalKeycode = keyEvent.PhysicalKeycode;
				
			InputMap.ActionAddEvent(Action, settingEvent);

			return true;
		}

		return false;
	}

	protected override void EraseMappings()
	{
		foreach (var @event in InputMap.ActionGetEvents(Action))
		{
			if (@event is InputEventKey)
				InputMap.ActionEraseEvent(Action, @event);
		}
	}
}