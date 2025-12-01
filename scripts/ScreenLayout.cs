using Godot;
using Godot.Collections;

namespace racingGame;

public partial class ScreenLayout : Control
{
	[Export] public Array<PlayerViewport> PlayerViewports;
}
