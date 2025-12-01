using Godot;

namespace racingGame;

public record struct InputDeviceJoypad : IInputDevice
{
	public int DeviceId;
	public string Name => Input.GetJoyName(DeviceId);
}