using Godot;

namespace racingGame;

public partial class SettingsMenu : Control
{
	private ConfigFile _configFile;

	[Export] public OptionButton Aa;

	[Export] public Slider MusicSlider;

	[Export] public string SettingsFilePath;
	[Export] public OptionButton Shadowq;
	[Export] public Slider SoundSlider;

	[Export] public Slider TDrs;
	[Export] public OptionButton Vsync;

	[Export] public OptionButton WinMode;

	public override void _Ready()
	{
		LoadSettings();

		SoundSlider.ValueChanged += _ => ApplySoundSettings();
		MusicSlider.ValueChanged += _ => ApplySoundSettings();
	}

	private void LoadSettings()
	{
		_configFile = new ConfigFile();

		GD.Print(_configFile.Load(SettingsFilePath));

		TDrs.Value = (double)_configFile.GetValue("graphics", "render_scale", 100);
		Aa.Selected = (int)_configFile.GetValue("graphics", "antialiasing", 0);
		Vsync.Selected = (int)_configFile.GetValue("graphics", "vsync", 1);
		WinMode.Selected = (int)_configFile.GetValue("graphics", "window_mode", 2);
		Shadowq.Selected = (int)_configFile.GetValue("graphics", "shadow_quality", 2);

		SoundSlider.Value = (double)_configFile.GetValue("audio", "sfx_level", 50);
		MusicSlider.Value = (double)_configFile.GetValue("audio", "music_level", 50);

		ApplySettings();
	}

	private void ApplySettings()
	{
		var viewport = GetViewport();
		var window = GetWindow();

		_configFile.SetValue("graphics", "render_scale", TDrs.Value);
		viewport.Scaling3DScale = (float)TDrs.Value * 0.01f;

		_configFile.SetValue("graphics", "antialiasing", Aa.Selected);
		viewport.ScreenSpaceAA = Viewport.ScreenSpaceAAEnum.Disabled;
		viewport.UseTaa = false;
		viewport.Msaa3D = Viewport.Msaa.Disabled;
		if (Aa.Selected == 1)
			viewport.ScreenSpaceAA = Viewport.ScreenSpaceAAEnum.Fxaa;
		else if (Aa.Selected == 2)
			viewport.UseTaa = true;
		else if (Aa.Selected == 3)
			viewport.Msaa3D = Viewport.Msaa.Msaa2X;
		else if (Aa.Selected == 4)
			viewport.Msaa3D = Viewport.Msaa.Msaa4X;
		else if (Aa.Selected == 5)
			viewport.Msaa3D = Viewport.Msaa.Msaa8X;

		_configFile.SetValue("graphics", "vsync", Vsync.Selected);
		DisplayServer.WindowSetVsyncMode((DisplayServer.VSyncMode)Vsync.Selected);

		_configFile.SetValue("graphics", "window_mode", WinMode.Selected);
		if (WinMode.Selected == 1)
			window.Mode = Window.ModeEnum.Windowed;
		else if (WinMode.Selected == 2)
			window.Mode = Window.ModeEnum.ExclusiveFullscreen;
		else
			window.Mode = Window.ModeEnum.Fullscreen;

		_configFile.SetValue("graphics", "shadow_quality", Shadowq.Selected);
		RenderingServer.DirectionalSoftShadowFilterSetQuality((RenderingServer.ShadowQuality)Shadowq.Selected);

		ApplySoundSettings();
	}

	private void ApplySoundSettings()
	{
		_configFile.SetValue("audio", "sfx_level", SoundSlider.Value);
		AudioServer.SetBusVolumeDb(1, (float)Mathf.LinearToDb(SoundSlider.Value * 0.01f));
		_configFile.SetValue("audio", "music_level", MusicSlider.Value);
		AudioServer.SetBusVolumeDb(2, (float)Mathf.LinearToDb(MusicSlider.Value * 0.01f));
	}

	private void SaveSettings()
	{
		GD.Print(_configFile.Save(SettingsFilePath));
	}

	public void OnBackButton()
	{
		ApplySettings();
		SaveSettings();
		Hide();
	}
}
