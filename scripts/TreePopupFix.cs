using Godot;

namespace racingGame;

public partial class TreePopupFix : Tree
{
	public override void _Ready()
	{
		foreach (var child in GetChildren(true))
			if (child is Popup popup)
				popup.Transparent = true;
	}
}