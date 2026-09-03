using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace racingGame.data;

public class Ghost
{
    public string PlayerName = "Ghost";
    public bool Empty = true;
    public TimeSpan RaceTime = TimeSpan.Zero;
    
    public List<GhostFrame> Frames = new List<GhostFrame>();
    public List<TimeSpan> CheckpointTimes = new List<TimeSpan>();
    public List<TimeSpan> LapTimes = new List<TimeSpan>();
    
    public void AddFrame(int raceTime, CarPositionData data)
    {
        Empty = false;
        Frames.Add(new GhostFrame(raceTime, data));
    }

    public CarPositionData GetFrame(int raceTime)
    {
        var returnFrame = Frames.First();
        int closestTime = 0;
        foreach (GhostFrame frame in Frames)
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