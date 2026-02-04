using Godot;

namespace racingGame;

public partial class VersionLabel : Label
{
	public override void _Ready()
	{
		Text = "Alpha " + ProjectSettings.GetSetting("application/config/version");
	}
}
