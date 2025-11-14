using System;
using System.Linq;
using Godot;
using Godot.Collections;

namespace racingGame;

public abstract partial class RemapButton : Button
{
	[Export] public StringName Action;

	private bool _isRemapping = false;

	protected abstract bool TryRemapEvent(InputEvent @event);

	protected abstract string FormatMappings(Array<InputEvent> events);

	protected abstract void EraseMappings();
	
	protected virtual string GetRemappingPrompt()
	{
		return $"{FormatMappings(InputMap
			.ActionGetEvents(Action))}, Press key...";
	}
	
	public override void _Ready()
	{
		LoadFromInputMap();
	}

	private void OnPressed(MouseButton mouseButton)
	{
		if (mouseButton != MouseButton.Left && mouseButton != MouseButton.Right)
			return;
		
		if (!_isRemapping)
		{
			if (mouseButton == MouseButton.Right)
			{
				EraseMappings();
				LoadFromInputMap();

				return;
			}
			
			_isRemapping = true;

			Text = GetRemappingPrompt();
		}
		else
		{
			_isRemapping = false;
			LoadFromInputMap();
		}
	}

	public void LoadFromInputMap()
	{
		Text = FormatMappings(InputMap
			.ActionGetEvents(Action));
		
		// prevents button from collapsing due to zero lines of text
		if (Text == "")
			Text = " ";
	}

	public override void _Input(InputEvent @event)
	{
		if (_isRemapping)
		{
			if (TryRemapEvent(@event))
			{
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