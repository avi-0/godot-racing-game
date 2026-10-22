using Godot;
using System;
using racingGame.data;

namespace racingGame;
public partial class AmbiencePlayer : Node
{
	[Export] private AudioStreamPlayer _windNormal;
	[Export] private AudioStreamPlayer _windStrong;
	[Export] private AudioStreamPlayer _rain;
	[Export] private AudioStreamPlayer _water;	
	
	public bool Playing = false;
	public Camera3D CurrentCamera;
	
	public override void _Ready()
	{
	}
	
	public override void _Process(double delta)
	{
		if (Playing && CurrentCamera != null)
		{
			if (!_water.Playing && CurrentCamera.GlobalPosition.Y < 0)
			{
				_water.Play();
			}
			else if (_water.Playing && CurrentCamera.GlobalPosition.Y >= 0)
			{
				_water.Stop();
			}
		}
	}

	public void StartPlaying(TrackOptions trackOptions)
	{
		StopPlaying();
		
		Playing = true;
		_windNormal.Playing = !trackOptions.Fog;
		_windStrong.Playing = trackOptions.Fog;
		_rain.Playing = trackOptions.Rain && trackOptions.TrackBase != "snow";
	}

	public void StopPlaying()
	{
		Playing = false;
		_windNormal.Stop();
		_windStrong.Stop();
		_rain.Stop();
		_water.Stop();
	}
}
