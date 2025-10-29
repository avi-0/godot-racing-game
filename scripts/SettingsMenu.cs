using Godot;
using System;

public partial class SettingsMenu : Control
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		LoadSettings();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	[Export] public Slider TDrs;
	[Export] public OptionButton AA;
	[Export] public OptionButton Vsync;
	[Export] public OptionButton WINMODE;
	[Export] public OptionButton SHADOWQ;

	[Export] public Slider MusicSlider;
	[Export] public Slider SoundSlider;

	private Godot.Collections.Dictionary<string, int> settings;
	private void DefaultSettings()
	{
		settings = new Godot.Collections.Dictionary<string, int>();
		settings.Add("render_scale", 100);
		settings.Add("aa", 0);
		settings.Add("vsync", 1);
		settings.Add("winmode", 2);
		settings.Add("shadowq", 2);

		settings.Add("gamesound", 50);
		settings.Add("musicsound", 50);
	}

	private void LoadSettings()
	{
		DefaultSettings();

		if (FileAccess.FileExists("user://settings.save"))
		{
			using var file = FileAccess.Open("user://settings.save", FileAccess.ModeFlags.Read);
			while (file.GetPosition() < file.GetLength())
			{
				var saveline = file.GetLine().Split('=');
				settings[saveline[0]] = saveline[1].ToInt();
			}
		}

		TDrs.Value = settings["render_scale"];
		AA.Selected = settings["aa"];
		Vsync.Selected = settings["vsync"];
		WINMODE.Selected = settings["winmode"];
		SHADOWQ.Selected = settings["shadowq"];

		SoundSlider.Value = settings["gamesound"];
		MusicSlider.Value = settings["musicsound"];

		ApplySettings();
	}

	private void ApplySettings()
	{
		settings["render_scale"] = (int)TDrs.Value;
		GetViewport().Scaling3DScale = settings["render_scale"]*0.01f;

		settings["aa"] = AA.Selected;
		GetViewport().ScreenSpaceAA = Viewport.ScreenSpaceAAEnum.Disabled;
		GetViewport().UseTaa = false;
		GetViewport().Msaa3D = Viewport.Msaa.Disabled;
		if (settings["aa"] == 1)
		{
			GetViewport().ScreenSpaceAA = Viewport.ScreenSpaceAAEnum.Fxaa;
		}
		else if (settings["aa"] == 2)
		{
			GetViewport().UseTaa = true;
		}
		else if (settings["aa"] == 3)
		{
			GetViewport().Msaa3D = Viewport.Msaa.Msaa2X;
		}
		else if (settings["aa"] == 4)
		{
			GetViewport().Msaa3D = Viewport.Msaa.Msaa4X;
		}		
		else if (settings["aa"] == 5)
		{
			GetViewport().Msaa3D = Viewport.Msaa.Msaa8X;
		}

		settings["vsync"] = Vsync.Selected;
		DisplayServer.WindowSetVsyncMode((DisplayServer.VSyncMode)settings["vsync"]);

		settings["winmode"] = WINMODE.Selected;
		if (settings["winmode"] == 1)
		{
			GetWindow().Mode = Window.ModeEnum.Windowed;
		}
		else if (settings["winmode"] == 2)
		{
			GetWindow().Mode = Window.ModeEnum.ExclusiveFullscreen;
		}
		else
		{
			GetWindow().Mode = Window.ModeEnum.Fullscreen;
		}

		settings["shadowq"] = SHADOWQ.Selected;
		RenderingServer.DirectionalSoftShadowFilterSetQuality((RenderingServer.ShadowQuality)settings["shadowq"]);

		settings["gamesound"] = (int)SoundSlider.Value;
		AudioServer.SetBusVolumeDb(1,(float)Mathf.LinearToDb(SoundSlider.Value*0.01f));
		settings["musicsound"] = (int)MusicSlider.Value;		
		AudioServer.SetBusVolumeDb(2,(float)Mathf.LinearToDb(MusicSlider.Value*0.01f));
	}

	private void SaveSettings()
	{
		using var saveFile = FileAccess.Open("user://settings.save", FileAccess.ModeFlags.Write);
		foreach (var settingline in settings)
		{
			saveFile.StoreLine(settingline.Key+"="+settingline.Value);
		}
	}

	public void OnBackButton()
	{
		ApplySettings();
		SaveSettings();
		this.Hide();
	}
}
