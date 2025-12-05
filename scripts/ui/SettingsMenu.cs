using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using racingGame.data;

namespace racingGame;

public partial class SettingsMenu : Control
{
	public readonly List<int> ShadowAtlasSizes = new()
	{
		0,
		256,
		512,
		1024,
		2048,
		4096,
		8192,
		16384,
	};
	
	[Export] public Slider TDrs;
	[Export] public OptionButton ScaleMode;
	[Export] public OptionButton WinMode;
	[Export] public OptionButton Aa;
	[Export] public OptionButton Vsync;
	[Export] public OptionButton ShadowFilterQuality;
	[Export] public OptionButton ShadowAtlasSize;
	
	[Export] public Slider SoundSlider;
	[Export] public Slider MusicSlider;

	[Export] public GridContainer ControlsContainer;

	[Export] public Button ResetGraphicsButton;
	[Export] public Button ResetSoundButton;
	[Export] public Button ResetControlsButton;
	[Export] public Button BackButton;

	public override void _Ready()
	{
		SoundSlider.DragEnded += _ => OnSoundSettingChanged();
		MusicSlider.DragEnded += _ => OnSoundSettingChanged();
		TDrs.DragEnded += _ => OnGraphicsSettingChanged();
		foreach (var optionButton in new List<OptionButton> {ScaleMode, WinMode, Aa, Vsync, ShadowFilterQuality, ShadowAtlasSize})
		{
			optionButton.ItemSelected += _ => OnGraphicsSettingChanged();
		}

		ResetGraphicsButton.Pressed += () =>
		{
			SettingsManager.Instance.Settings.Graphics = new();
			UpdateUiFromSettings();
		};
		ResetSoundButton.Pressed += () =>
		{
			SettingsManager.Instance.Settings.Sound = new();
			UpdateUiFromSettings();
		};
		ResetControlsButton.Pressed += () =>
		{
			InputMap.LoadFromProjectSettings();
			SettingsManager.Instance.UpdateSettingsFromInputMap();
			UpdateUiFromSettings();
		};
		BackButton.Pressed += OnBackButton;

		foreach (var size in ShadowAtlasSizes)
		{
			var text = size == 0 ? "None" : size.ToString();
			ShadowAtlasSize.AddItem(text, size);
		}
	}
	
	private void UpdateUiFromSettings()
	{
		var settings = SettingsManager.Instance.Settings;
		
		TDrs.Value = settings.Graphics.RenderScale;
		ScaleMode.Selected = settings.Graphics.ScaleMode;
		Aa.Selected = settings.Graphics.Antialiasing;
		Vsync.Selected = settings.Graphics.Vsync;
		WinMode.Selected = settings.Graphics.WindowMode;
		ShadowFilterQuality.Selected = settings.Graphics.ShadowFilterQuality;
		ShadowAtlasSize.Selected = ShadowAtlasSize.GetItemIndex(settings.Graphics.ShadowAtlasSize);

		SoundSlider.Value = settings.Sound.SfxLevel;
		MusicSlider.Value = settings.Sound.MusicLevel;

		SettingsManager.Instance.UpdateInputMapFromSettings();
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
		var settings = SettingsManager.Instance.Settings;
		
		settings.Graphics.RenderScale = TDrs.Value;
		settings.Graphics.ScaleMode = ScaleMode.Selected;
		settings.Graphics.Antialiasing = Aa.Selected;
		settings.Graphics.Vsync = Vsync.Selected;
		settings.Graphics.WindowMode = WinMode.Selected;
		settings.Graphics.ShadowFilterQuality = ShadowFilterQuality.Selected;
		settings.Graphics.ShadowAtlasSize = ShadowAtlasSize.GetSelectedId();

		settings.Sound.SfxLevel = SoundSlider.Value;
		settings.Sound.MusicLevel = MusicSlider.Value;

		SettingsManager.Instance.UpdateSettingsFromInputMap();
	}

	public void OnBackButton()
	{
		UpdateSettingsFromUi();
		SettingsManager.Instance.ApplySettings();
		SettingsManager.Instance.SaveSettings();
		Hide();
	}

	private void OnGraphicsSettingChanged()
	{
		UpdateSettingsFromUi();
		SettingsManager.Instance.ApplyGraphicsSettings();
	}

	private void OnSoundSettingChanged()
	{
		UpdateSettingsFromUi();
		SettingsManager.Instance.ApplySoundSettings();
	}

	public void OnVisibilityChanged()
	{
		if (Visible)
		{
			BackButton.GrabFocus();
			
			SettingsManager.Instance.LoadSettings();
			UpdateUiFromSettings();
		}
	}
}
