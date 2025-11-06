using Godot;

namespace racingGame;

public struct TimeAttackMap
{
	public Track Track { get; init; }
	public int CheckPointCount { get; set; } = 0;

	public TimeAttackMap(Track track)
	{
		Track = track;
	}
}