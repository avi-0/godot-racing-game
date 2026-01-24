using Godot;

namespace racingGame;

public partial class VersionLabel : Label
{
	public override void _Ready()
	{
		Text = "a" + ProjectSettings.GetSetting("application/config/version");
	}
}
