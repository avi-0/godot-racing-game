using Godot;

namespace racingGame;

public partial class SettingsMenu : Control
{
	private GameSettings _settings;

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
		_settings = Jz.Load<GameSettings>(SettingsFilePath) ?? new GameSettings();

		TDrs.Value = _settings.RenderScale;
		Aa.Selected = _settings.Antialiasing;
		Vsync.Selected = _settings.Vsync;
		WinMode.Selected = _settings.WindowMode;
		Shadowq.Selected = _settings.ShadowQuality;

		SoundSlider.Value = _settings.SfxLevel;
		MusicSlider.Value = _settings.MusicLevel;

		ApplySettings();
	}

	private void ApplySettings()
	{
		var viewport = GetViewport();
		var window = GetWindow();

		_settings.RenderScale = TDrs.Value;
		viewport.Scaling3DScale = (float)_settings.RenderScale * 0.01f;
		
		_settings.Antialiasing = Aa.Selected;
		viewport.ScreenSpaceAA = Viewport.ScreenSpaceAAEnum.Disabled;
		viewport.UseTaa = false;
		viewport.Msaa3D = Viewport.Msaa.Disabled;
		if (_settings.Antialiasing == 1)
			viewport.ScreenSpaceAA = Viewport.ScreenSpaceAAEnum.Fxaa;
		else if (_settings.Antialiasing == 2)
			viewport.UseTaa = true;
		else if (_settings.Antialiasing == 3)
			viewport.Msaa3D = Viewport.Msaa.Msaa2X;
		else if (_settings.Antialiasing == 4)
			viewport.Msaa3D = Viewport.Msaa.Msaa4X;
		else if (_settings.Antialiasing == 5)
			viewport.Msaa3D = Viewport.Msaa.Msaa8X;
		
		_settings.Vsync = Vsync.Selected;
		DisplayServer.WindowSetVsyncMode((DisplayServer.VSyncMode)_settings.Vsync);
		
		_settings.WindowMode = WinMode.Selected;
		if (_settings.WindowMode == 1)
			window.Mode = Window.ModeEnum.Windowed;
		else if (_settings.WindowMode == 2)
			window.Mode = Window.ModeEnum.ExclusiveFullscreen;
		else
			window.Mode = Window.ModeEnum.Fullscreen;

		_settings.ShadowQuality = Shadowq.Selected;
		RenderingServer.DirectionalSoftShadowFilterSetQuality((RenderingServer.ShadowQuality)_settings.ShadowQuality);

		ApplySoundSettings();
	}

	private void ApplySoundSettings()
	{
		_settings.SfxLevel = SoundSlider.Value;
		_settings.MusicLevel = MusicSlider.Value;
		
		AudioServer.SetBusVolumeDb(1, (float)Mathf.LinearToDb(_settings.SfxLevel * 0.01f));
		AudioServer.SetBusVolumeDb(2, (float)Mathf.LinearToDb(_settings.MusicLevel * 0.01f));
	}

	private void SaveSettings()
	{
		Jz.Save(SettingsFilePath, _settings);
	}

	public void OnBackButton()
	{
		ApplySettings();
		SaveSettings();
		Hide();
	}
}
