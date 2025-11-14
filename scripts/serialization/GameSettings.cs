using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

namespace racingGame;

public class GameSettings
{
	public double RenderScale = 100;
	public int ScaleMode = 1;
	public int Antialiasing = 0;
	public int Vsync = 1;
	public int WindowMode = 2;
	public int ShadowQuality = 2;
	public double SfxLevel = 50;
	public double MusicLevel = 50;

	public Dictionary<string, List<InputEventData>> InputMap = new();
}