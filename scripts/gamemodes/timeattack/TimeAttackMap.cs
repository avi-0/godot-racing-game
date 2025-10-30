using Godot;

public struct TimeAttackMap
{
    public Node3D TrackNode {get; init;}
    public int CheckPointCount {get; set;} = 0;

    public TimeAttackMap(Node3D trackNode)
    {
        TrackNode = trackNode;
    }
}