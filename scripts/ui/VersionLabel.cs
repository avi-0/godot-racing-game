using Godot;

namespace racingGame;

public partial class VersionLabel : Label
{
	public override void _Ready()
	{
		Text = ProjectSettings.GetSetting("application/config/version").ToString();
	}
}
