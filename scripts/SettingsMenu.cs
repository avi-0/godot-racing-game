using Godot;
using Godot.Collections;

namespace racingGame;

public partial class SettingsMenu : Control
{
	private Dictionary<string, int> _settings;
	[Export] public OptionButton Aa;

	[Export] public Slider MusicSlider;
	[Export] public OptionButton Shadowq;
	[Export] public Slider SoundSlider;

	[Export] public Slider TDrs;
	[Export] public OptionButton Vsync;

	[Export] public OptionButton WinMode;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		LoadSettings();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void DefaultSettings()
	{
		_settings = new Dictionary<string, int>();
		_settings.Add("render_scale", 100);
		_settings.Add("aa", 0);
		_settings.Add("vsync", 1);
		_settings.Add("winmode", 2);
		_settings.Add("shadowq", 2);

		_settings.Add("gamesound", 50);
		_settings.Add("musicsound", 50);
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
				_settings[saveline[0]] = saveline[1].ToInt();
			}
		}

		TDrs.Value = _settings["render_scale"];
		Aa.Selected = _settings["aa"];
		Vsync.Selected = _settings["vsync"];
		WinMode.Selected = _settings["winmode"];
		Shadowq.Selected = _settings["shadowq"];

		SoundSlider.Value = _settings["gamesound"];
		MusicSlider.Value = _settings["musicsound"];

		ApplySettings();
	}

	private void ApplySettings()
	{
		_settings["render_scale"] = (int)TDrs.Value;
		GetViewport().Scaling3DScale = _settings["render_scale"] * 0.01f;

		_settings["aa"] = Aa.Selected;
		GetViewport().ScreenSpaceAA = Viewport.ScreenSpaceAAEnum.Disabled;
		GetViewport().UseTaa = false;
		GetViewport().Msaa3D = Viewport.Msaa.Disabled;
		if (_settings["aa"] == 1)
			GetViewport().ScreenSpaceAA = Viewport.ScreenSpaceAAEnum.Fxaa;
		else if (_settings["aa"] == 2)
			GetViewport().UseTaa = true;
		else if (_settings["aa"] == 3)
			GetViewport().Msaa3D = Viewport.Msaa.Msaa2X;
		else if (_settings["aa"] == 4)
			GetViewport().Msaa3D = Viewport.Msaa.Msaa4X;
		else if (_settings["aa"] == 5) GetViewport().Msaa3D = Viewport.Msaa.Msaa8X;

		_settings["vsync"] = Vsync.Selected;
		DisplayServer.WindowSetVsyncMode((DisplayServer.VSyncMode)_settings["vsync"]);

		_settings["winmode"] = WinMode.Selected;
		if (_settings["winmode"] == 1)
			GetWindow().Mode = Window.ModeEnum.Windowed;
		else if (_settings["winmode"] == 2)
			GetWindow().Mode = Window.ModeEnum.ExclusiveFullscreen;
		else
			GetWindow().Mode = Window.ModeEnum.Fullscreen;

		_settings["shadowq"] = Shadowq.Selected;
		RenderingServer.DirectionalSoftShadowFilterSetQuality((RenderingServer.ShadowQuality)_settings["shadowq"]);

		_settings["gamesound"] = (int)SoundSlider.Value;
		AudioServer.SetBusVolumeDb(1, (float)Mathf.LinearToDb(SoundSlider.Value * 0.01f));
		_settings["musicsound"] = (int)MusicSlider.Value;
		AudioServer.SetBusVolumeDb(2, (float)Mathf.LinearToDb(MusicSlider.Value * 0.01f));
	}

	private void SaveSettings()
	{
		using var saveFile = FileAccess.Open("user://settings.save", FileAccess.ModeFlags.Write);
		foreach (var settingline in _settings) saveFile.StoreLine(settingline.Key + "=" + settingline.Value);
	}

	public void OnBackButton()
	{
		ApplySettings();
		SaveSettings();
		Hide();
	}
}
