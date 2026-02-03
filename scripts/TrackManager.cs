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

		UpdateShadowsEnabled();

		GameModeController.LoadMap(Track);
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

	public Image GetTrackImage(TrackOptions trackOptions)
	{
		Image image = new Image();
		if (image.LoadJpgFromBuffer(Marshalls.Base64ToRaw(trackOptions.PreviewImage)) != Error.Ok)
		{
			image = Image.CreateEmpty(512, 512, true, Image.Format.Rgb8);
		}

		Image icon = new Image();
		icon.CopyFrom(image);
		return icon;
	}

	public void NewTrack()
	{
		Track.Load(new TrackData());
	}
	
	public string GetLoadedTrackUid()
	{
		return Track.Options.Uid;
	}
	
	public void UpdateShadowsEnabled()
	{
		Track.UpdateLighting();
	}
}
