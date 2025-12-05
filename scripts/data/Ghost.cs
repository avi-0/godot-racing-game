using System.Collections.Generic;
using System.Linq;
using Godot;

namespace racingGame.data;

public class Ghost
{
    public string PlayerName = "Ghost";
    public bool Empty = true;

    private List<GhostFrame> _frames = new List<GhostFrame>();
    
    public void AddFrame(int raceTime, CarPositionData data)
    {
        Empty = false;
        _frames.Add(new GhostFrame(raceTime, data));
    }

    public CarPositionData GetFrame(int raceTime)
    {
        var returnFrame = _frames.First();
        int closestTime = 0;
        foreach (GhostFrame frame in _frames)
        {
            if (Mathf.Abs(frame.RaceTime - raceTime) < Mathf.Abs(closestTime - raceTime))
            {
                returnFrame = frame;
                closestTime = frame.RaceTime;
            }
        }

        return returnFrame.Data;
    }
}

public struct GhostFrame(int raceTime, CarPositionData data)
{
    public int RaceTime = raceTime;
    public CarPositionData Data = data;
}

public struct CarPositionData(Vector3 position, Vector3 rotation)
{
    public Vector3 Position = position;
    public Vector3 Rotation = rotation;
}