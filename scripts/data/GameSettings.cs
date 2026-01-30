using System.Collections.Generic;

namespace racingGame.data;

public class GameSettings
{
	public GraphicsSettings Graphics = new();
	public SoundSettings Sound = new();
	public string PlayerName = "Player";
	public Dictionary<string, List<InputEventData>> InputMap = new();
	public Dictionary<string, int> SelectedSkins = new();
	public bool PerfMonEnabled = false;
	
	public class GraphicsSettings
	{
		public double RenderScale = 100;
		public int ScaleMode = 1;
		public int Antialiasing = 0;
		public int Vsync = 1;
		public int WindowMode = 2;
		public int ShadowQuality = 4;
	}

	public class SoundSettings
	{
		public double SfxLevel = 50;
		public double MusicLevel = 40;
	}
}