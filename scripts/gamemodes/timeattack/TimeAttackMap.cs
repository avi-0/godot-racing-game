using Godot;

public struct TimeAttackMap
{
    public Node3D TrackNode {get; init;}
    public string TrackUID {get; init;}
    public int CheckPointCount {get; set;} = 0;
    public int LapsCount {get; set;} = 0;
    public int AuthorTime {get; set;} = 0;

    public string MapType {get; set;} = "";
    public string TrackName {get; set;} = "";
    public string AuthorName {get; set;} = "";
    
    public TimeAttackMap(string trackUid, Node3D trackNode)
    {
        TrackUID = trackUid;
        TrackNode = trackNode;
    }
}