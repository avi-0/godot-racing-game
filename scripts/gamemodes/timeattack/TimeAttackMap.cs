using Godot;

namespace racingGame;

public struct TimeAttackMap
{
	public Node3D TrackNode { get; init; }
	public string TrackUid { get; init; }
	public int CheckPointCount { get; set; } = 0;
	public int LapsCount { get; set; } = 0;
	public int AuthorTime { get; set; } = 0;

	public string MapType { get; set; } = "";
	public string TrackName { get; set; } = "";
	public string AuthorName { get; set; } = "";

	public TimeAttackMap(string trackUid, Node3D trackNode)
	{
		TrackUid = trackUid;
		TrackNode = trackNode;
	}
}