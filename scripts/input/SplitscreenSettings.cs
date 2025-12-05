using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using racingGame;

public partial class SplitscreenSettings : Control
{
	[Export] public Label DevicesLabel;
	[Export] public Button JoinKeyboardButton;
	[Export] public Button LayoutSingleButton;
	[Export] public Button Layout2HButton;
	[Export] public Button Layout2VButton;
	[Export] public Button Layout3HButton;
	[Export] public Button Layout3VButton;
	[Export] public Button Layout4Button;
	private List<Button> _layoutButtons;
	private Dictionary<Button, PackedScene> _layouts;
	
	public override void _Ready()
	{
		InputManager.Instance.DevicesChanged += OnDevicesChanged;
		JoinKeyboardButton.Pressed += JoinKeyboardButtonOnPressed;

		_layoutButtons = new()
		{
			LayoutSingleButton,
			Layout2HButton,
			Layout2VButton,
			Layout3HButton,
			Layout3VButton,
			Layout4Button,
		};
		_layouts = new();
		_layouts[LayoutSingleButton] = GameManager.Instance.SingleplayerScreenLayout;
		_layouts[Layout2HButton] = GameManager.Instance.SplitScreen2HLayout;
		_layouts[Layout2VButton] = GameManager.Instance.SplitScreen2VLayout;
		_layouts[Layout3HButton] = GameManager.Instance.SplitScreen3HLayout;
		_layouts[Layout3VButton] = GameManager.Instance.SplitScreen3VLayout;
		_layouts[Layout4Button] = GameManager.Instance.SplitScreen4Layout;
		
		foreach (var button in _layoutButtons)
		{
			button.Toggled += (on) => ButtonOnToggled(button, on);
		}
		
		OnDevicesChanged();
	}

	private void ButtonOnToggled(Button button, bool toggledOn)
	{
		if (toggledOn)
		{
			GameManager.Instance.SetScreenLayout(_layouts[button]);
		}
	}

	private void JoinKeyboardButtonOnPressed()
	{
		InputManager.Instance.ToggleDevice(new InputDeviceKeyboard());
	}

	private void OnDevicesChanged()
	{
		GD.Print($"[{string.Join(", ", InputManager.Instance.Devices)}]");

		DevicesLabel.Text = "Players:\n";
		if (InputManager.Instance.Devices.Count > 0)
		{
			var names = InputManager.Instance.Devices
				.Select((device, i) => $"  {i + 1} {device.Name}");
			DevicesLabel.Text += string.Join("\n", names);
		}
		else
		{
			DevicesLabel.Text += "  None";
		}

		var numDevices = Math.Clamp(InputManager.Instance.Devices.Count, 1, 4);
		foreach (var button in _layoutButtons)
		{
			button.Disabled = true;
		}
		if (numDevices == 1)
		{
			LayoutSingleButton.Disabled = false;
		}
		else if (numDevices == 2)
		{
			Layout2HButton.Disabled = false;
			Layout2VButton.Disabled = false;
		} else if (numDevices == 3)
		{
			Layout3HButton.Disabled = false;
			Layout3VButton.Disabled = false;
		}
		else
		{
			Layout4Button.Disabled = false;
		}

		if (!_layoutButtons.Exists(button => !button.Disabled && button.IsPressed()))
		{
			var button = _layoutButtons.First(button => !button.Disabled);
			button.SetPressed(true);
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (GameManager.Instance.IsPlaying())
			return;
		
		if (@event.IsActionPressed("game_pause"))
		{
			var device = InputManager.GetDevice(@event);
			if (device != null && device is not InputDeviceKeyboard)
			{
				InputManager.Instance.ToggleDevice(device);
			}
		}
	}
}
