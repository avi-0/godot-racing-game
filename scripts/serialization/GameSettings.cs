using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

namespace racingGame;

public class GameSettings
{
	public GraphicsSettings Graphics = new();
	public SoundSettings Sound = new();
	public string PlayerName = "Player";
	public Dictionary<string, List<InputEventData>> InputMap = new();

	public class GraphicsSettings
	{
		public double RenderScale = 100;
		public int ScaleMode = 1;
		public int Antialiasing = 0;
		public int Vsync = 1;
		public int WindowMode = 2;
		public int ShadowFilterQuality = 2;
		public int ShadowAtlasSize = 4096;
	}

	public class SoundSettings
	{
		public double SfxLevel = 50;
		public double MusicLevel = 50;
	}
}