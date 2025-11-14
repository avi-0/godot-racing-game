using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace racingGame;

public partial class SettingsMenu : Control
{
	public readonly List<StringName> ConfigurableActions = new()
	{
		InputActionNames.Forward,
		InputActionNames.Back,
		InputActionNames.Left,
		InputActionNames.Right,
		InputActionNames.Restart,
		InputActionNames.CycleCamera,
		InputActionNames.ToggleLights,
	};
	
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

	[Export] public GridContainer ControlsContainer;

	[Export] public Button ResetGraphicsButton;
	[Export] public Button ResetSoundButton;
	[Export] public Button ResetControlsButton;

	public override void _Ready()
	{
		LoadSettings();
		UpdateUiFromSettings();
		ApplySettings();

		SoundSlider.DragEnded += _ => OnSoundSettingChanged();
		MusicSlider.DragEnded += _ => OnSoundSettingChanged();

		ResetGraphicsButton.Pressed += () =>
		{
			_settings.Graphics = new();
			UpdateUiFromSettings();
		};
		ResetSoundButton.Pressed += () =>
		{
			_settings.Sound = new();
			UpdateUiFromSettings();
		};
		ResetControlsButton.Pressed += () =>
		{
			InputMap.LoadFromProjectSettings();
			UpdateSettingsFromInputMap();
			UpdateUiFromSettings();
		};
	}

	private void LoadSettings()
	{
		_settings = Jz.Load<GameSettings>(SettingsFilePath) ?? new GameSettings();
	}
	
	private void UpdateUiFromSettings()
	{
		TDrs.Value = _settings.Graphics.RenderScale;
		ScaleMode.Selected = _settings.Graphics.ScaleMode;
		Aa.Selected = _settings.Graphics.Antialiasing;
		Vsync.Selected = _settings.Graphics.Vsync;
		WinMode.Selected = _settings.Graphics.WindowMode;
		Shadowq.Selected = _settings.Graphics.ShadowQuality;

		SoundSlider.Value = _settings.Sound.SfxLevel;
		MusicSlider.Value = _settings.Sound.MusicLevel;

		UpdateInputMapFromSettings();
		foreach (var control in ControlsContainer.GetChildren())
		{
			if (control is RemapButton button)
			{
				button.LoadFromInputMap();
			}
		}
	}
	
	private void UpdateSettingsFromUi()
	{
		_settings.Graphics.RenderScale = TDrs.Value;
		_settings.Graphics.ScaleMode = ScaleMode.Selected;
		_settings.Graphics.Antialiasing = Aa.Selected;
		_settings.Graphics.Vsync = Vsync.Selected;
		_settings.Graphics.WindowMode = WinMode.Selected;
		_settings.Graphics.ShadowQuality = Shadowq.Selected;

		_settings.Sound.SfxLevel = SoundSlider.Value;
		_settings.Sound.MusicLevel = MusicSlider.Value;

		UpdateSettingsFromInputMap();
	}

	private void UpdateSettingsFromInputMap()
	{
		_settings.InputMap = new();
		foreach (var actionName in ConfigurableActions)
		{
			_settings.InputMap[actionName] = InputMap
				.ActionGetEvents(actionName)
				.Select(@event => InputEventData.Save(@event))
				.Where(data => data != null)
				.ToList();
		}
	}

	private void UpdateInputMapFromSettings()
	{
		foreach (var actionName in ConfigurableActions)
		{
			if (_settings.InputMap.ContainsKey(actionName))
			{
				InputMap.ActionEraseEvents(actionName);
				foreach (var @event in _settings.InputMap[actionName])
				{
					InputMap.ActionAddEvent(actionName, @event.Load());
				}
			}
		}
	}

	private void ApplySettings()
	{
		var viewport = GetViewport();
		var window = GetWindow();

		viewport.SetScaling3DMode((Viewport.Scaling3DModeEnum)_settings.Graphics.ScaleMode);
		
		viewport.Scaling3DScale = (float)_settings.Graphics.RenderScale * 0.01f;
		
		viewport.ScreenSpaceAA = Viewport.ScreenSpaceAAEnum.Disabled;
		viewport.UseTaa = false;
		viewport.Msaa3D = Viewport.Msaa.Disabled;
		if (_settings.Graphics.Antialiasing == 1)
			viewport.ScreenSpaceAA = Viewport.ScreenSpaceAAEnum.Fxaa;
		else if (_settings.Graphics.Antialiasing == 2)
			viewport.UseTaa = true;
		else if (_settings.Graphics.Antialiasing == 3)
			viewport.Msaa3D = Viewport.Msaa.Msaa2X;
		else if (_settings.Graphics.Antialiasing == 4)
			viewport.Msaa3D = Viewport.Msaa.Msaa4X;
		else if (_settings.Graphics.Antialiasing == 5)
			viewport.Msaa3D = Viewport.Msaa.Msaa8X;
		
		DisplayServer.WindowSetVsyncMode((DisplayServer.VSyncMode)_settings.Graphics.Vsync);

		if (_settings.Graphics.WindowMode == 1)
		{
			window.Mode = Window.ModeEnum.Maximized;
		}
		else if (_settings.Graphics.WindowMode == 2)
			window.Mode = Window.ModeEnum.ExclusiveFullscreen;
		else
			window.Mode = Window.ModeEnum.Fullscreen;
		
		RenderingServer.DirectionalSoftShadowFilterSetQuality((RenderingServer.ShadowQuality)_settings.Graphics.ShadowQuality);

		ApplySoundSettings();
	}

	private void ApplySoundSettings()
	{
		AudioServer.SetBusVolumeDb(1, (float)Mathf.LinearToDb(_settings.Sound.SfxLevel * 0.01f));
		AudioServer.SetBusVolumeDb(2, (float)Mathf.LinearToDb(_settings.Sound.MusicLevel * 0.01f));
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
