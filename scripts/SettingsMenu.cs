using Godot;

namespace racingGame;

public partial class SettingsMenu : Control
{
	private GameSettings _settings;

	[Export] public OptionButton Aa;
	[Export] public OptionButton ScaleMode;
	
	[Export] public Slider MusicSlider;

	[Export] public string SettingsFilePath;
	[Export] public OptionButton Shadowq;
	[Export] public Slider SoundSlider;

	[Export] public Slider TDrs;
	[Export] public OptionButton Vsync;

	[Export] public OptionButton WinMode;

	[Export] public Button ThrottleKB;
	[Export] public Button BrakeKB;
	[Export] public Button LeftKB;
	[Export] public Button RightKB;
	[Export] public Button RestartKB;
	[Export] public Button CameraKB;
	[Export] public Button LightsKB;

	public override void _Ready()
	{
		LoadSettings();
		UpdateUiFromSettings();
		ApplySettings();

		SoundSlider.ValueChanged += _ => OnSoundSettingChanged();
		MusicSlider.ValueChanged += _ => OnSoundSettingChanged();
	}

	private void LoadSettings()
	{
		_settings = Jz.Load<GameSettings>(SettingsFilePath) ?? new GameSettings();
	}
	
	private void UpdateUiFromSettings()
	{
		TDrs.Value = _settings.RenderScale;
		ScaleMode.Selected = _settings.ScaleMode;
		Aa.Selected = _settings.Antialiasing;
		Vsync.Selected = _settings.Vsync;
		WinMode.Selected = _settings.WindowMode;
		Shadowq.Selected = _settings.ShadowQuality;

		SoundSlider.Value = _settings.SfxLevel;
		MusicSlider.Value = _settings.MusicLevel;

		ThrottleKB.Text = GetActionButtons("throttle");
		BrakeKB.Text = GetActionButtons("brake");
		LeftKB.Text = GetActionButtons("steer_left");
		RightKB.Text = GetActionButtons("steer_right");
		CameraKB.Text = GetActionButtons("camera_switch");
		LightsKB.Text = GetActionButtons("lights_switch");
	}

	private string GetActionButtons(string actionEventName)
	{
		var retrunString = "";

		foreach (InputEvent inputEvent in InputMap.ActionGetEvents(actionEventName))
		{
			if (retrunString != "")
			{
				retrunString += ", ";
			}

			retrunString += inputEvent.AsText().Replace("(Physical)", "").Trim();
		}
		
		return retrunString;
	}
	
	private void UpdateSettingsFromUi()
	{
		_settings.RenderScale = TDrs.Value;
		_settings.ScaleMode = ScaleMode.Selected;
		_settings.Antialiasing = Aa.Selected;
		_settings.Vsync = Vsync.Selected;
		_settings.WindowMode = WinMode.Selected;
		_settings.ShadowQuality = Shadowq.Selected;

		_settings.SfxLevel = SoundSlider.Value;
		_settings.MusicLevel = MusicSlider.Value;
	}

	private void ApplySettings()
	{
		var viewport = GetViewport();
		var window = GetWindow();

		viewport.SetScaling3DMode((Viewport.Scaling3DModeEnum)_settings.ScaleMode);
		
		viewport.Scaling3DScale = (float)_settings.RenderScale * 0.01f;
		
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
		
		DisplayServer.WindowSetVsyncMode((DisplayServer.VSyncMode)_settings.Vsync);

		if (_settings.WindowMode == 1)
		{
			window.Mode = Window.ModeEnum.Maximized;
		}
		else if (_settings.WindowMode == 2)
			window.Mode = Window.ModeEnum.ExclusiveFullscreen;
		else
			window.Mode = Window.ModeEnum.Fullscreen;
		
		RenderingServer.DirectionalSoftShadowFilterSetQuality((RenderingServer.ShadowQuality)_settings.ShadowQuality);

		ApplySoundSettings();
	}

	private void ApplySoundSettings()
	{
		AudioServer.SetBusVolumeDb(1, (float)Mathf.LinearToDb(_settings.SfxLevel * 0.01f));
		AudioServer.SetBusVolumeDb(2, (float)Mathf.LinearToDb(_settings.MusicLevel * 0.01f));
	}

	private void SaveSettings()
	{
		Jz.Save(SettingsFilePath, _settings);
	}

	public void OnBackButton()
	{
		UpdateSettingsFromUi();
		ApplySettings();
		SaveSettings();
		Hide();
	}

	private void OnSoundSettingChanged()
	{
		UpdateSettingsFromUi();
		ApplySoundSettings();
	}

	public string GetLocalPlayerName()
	{
		return _settings.PlayerName;
	}

	public void SetLocalPlayerName(string name)
	{
		_settings.PlayerName = name;
		SaveSettings();
	}
}
