using System.Collections.Generic;
using Godot;

namespace racingGame;

public partial class InputManager : Node
{
	public static InputManager Instance;

	[Signal]
	public delegate void DevicesChangedEventHandler();
	
	
	private List<IInputDevice> _devices = new();

	public List<IInputDevice> Devices => _devices;

	public override void _EnterTree()
	{
		Instance = this;
	}

	public static IInputDevice GetDevice(InputEvent @event)
	{
		if (@event is InputEventJoypadButton or InputEventJoypadMotion)
		{
			return new InputDeviceJoypad
			{
				DeviceId = @event.Device,
			};
		}

		if (@event is InputEventKey)
		{
			return new InputDeviceKeyboard();
		}

		return null;
	}

	public void ToggleDevice(IInputDevice device)
	{
		if (_devices.Contains(device))
		{
			_devices.Remove(device);
		}
		else
		{
			_devices.Add(device);
		}
		EmitSignalDevicesChanged();
	}

	public bool InputEventMatchesPlayer(InputEvent @event, int player)
	{
		if (_devices.Count == 0)
			return true;

		if (player < 0 || player >= _devices.Count)
			return false;

		var device = _devices[player];
		return GetDevice(@event)?.Equals(device) ?? false;
	}
}
