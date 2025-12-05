using System;
using System.Linq;
using Godot;
using racingGame.data;

namespace racingGame;

public partial class TrackManager : Node
{
	public static TrackManager Instance;
	
	
	[Export] public Track Track;
	
	
	public override void _Ready()
	{
		Instance = this;
		
		NewTrack();
	}
	
	public Transform3D GetStartPoint()
	{
		foreach (var block in Track.FindChildren("*", "Block", false).Cast<Block>())
			if (block.IsStart)
				return block.SpawnPoint;

		return Transform3D.Identity;
	}
	
	public void OpenTrack(string path)
	{
		GD.Print($"Opening track at {path}");
		
		Track.Load(Jz.Load<TrackData>(path));

		ApplyShadowSettings();

		GameModeController.CurrentGameMode.InitTrack(Track);
		GD.Print("Track UID: " + GetLoadedTrackUid());
	}
	
	public void SaveTrack(string path)
	{
		GD.Print($"Saving track as {path}");

		Track.Options.Uid = Guid.NewGuid().ToString();
		
		GD.Print($"New Track UID: {GetLoadedTrackUid()}");
		
		Jz.Save(path, Track.Save());
	}
	
	public TrackOptions GetTrackOptions(string path)
	{
		try
		{
			var data = Jz.Load<TrackData>(path);

			return data.Options;
		}
		catch (Exception e)
		{
			GD.PushError(e);
			return null;
		}
	}

	public void NewTrack()
	{
		Track.GetNode("Sky3D").GetNode("TimeOfDay").Set("current_time", 10.0f);
		Track.Load(new TrackData());
	}
	
	public string GetLoadedTrackUid()
	{
		return Track.Options.Uid;
	}
	
	public void ApplyShadowSettings()
	{
		GetTree().SetGroup("light_directional_shadow", "shadow_enabled", GameManager.Instance.DirectionalShadowsEnabled);
	}
}
