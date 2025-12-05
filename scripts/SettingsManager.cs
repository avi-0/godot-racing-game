using System.Collections.Generic;
using System.Linq;
using Godot;
using racingGame.data;

namespace racingGame;

public partial class SettingsManager : Node
{
	public static SettingsManager Instance;
	
	
	[Export] public string SettingsFilePath;
	
	
	public GameSettings Settings;
	
	
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
	
	
	public override void _Ready()
	{
		Instance = this;
		
		LoadSettings();
		ApplySettings();
	}
	
	public void LoadSettings()
	{
		Settings = Jz.Load<GameSettings>(SettingsFilePath) ?? new GameSettings();
	}
	
	public void SaveSettings()
	{
		Jz.Save(SettingsFilePath, Settings);
	}
	
	public void UpdateSettingsFromInputMap()
	{
		Settings.InputMap = new();
		foreach (var actionName in ConfigurableActions)
		{
			Settings.InputMap[actionName] = InputMap
				.ActionGetEvents(actionName)
				.Select(@event => InputEventData.Save(@event))
				.Where(data => data != null)
				.ToList();
		}
	}
	
	public void UpdateInputMapFromSettings()
	{
		foreach (var actionName in ConfigurableActions)
		{
			if (Settings.InputMap.ContainsKey(actionName))
			{
				InputMap.ActionEraseEvents(actionName);
				foreach (var @event in Settings.InputMap[actionName])
				{
					InputMap.ActionAddEvent(actionName, @event.Load());
				}
			}
		}
	}
	
	public string GetLocalPlayerName()
	{
		return Settings.PlayerName;
	}
	
	public void SetLocalPlayerName(string name)
	{
		Settings.PlayerName = name;
		SaveSettings();
	}
	
	public void ApplySettings()
	{
		ApplyGraphicsSettings();
		ApplySoundSettings();
	}

	public void ApplySoundSettings()
	{
		AudioServer.SetBusVolumeDb(1, (float)Mathf.LinearToDb(Settings.Sound.SfxLevel * 0.01f));
		AudioServer.SetBusVolumeDb(2, (float)Mathf.LinearToDb(Settings.Sound.MusicLevel * 0.01f));
	}
	
	public void ApplyGraphicsSettings()
	{
		var viewport = GetViewport();
		var window = GetWindow();

		viewport.SetScaling3DMode((Viewport.Scaling3DModeEnum)Settings.Graphics.ScaleMode);
		
		viewport.Scaling3DScale = (float)Settings.Graphics.RenderScale * 0.01f;
		
		viewport.ScreenSpaceAA = Viewport.ScreenSpaceAAEnum.Disabled;
		viewport.UseTaa = false;
		viewport.Msaa3D = Viewport.Msaa.Disabled;
		if (Settings.Graphics.Antialiasing == 1)
			viewport.ScreenSpaceAA = Viewport.ScreenSpaceAAEnum.Fxaa;
		else if (Settings.Graphics.Antialiasing == 2)
			viewport.UseTaa = true;
		else if (Settings.Graphics.Antialiasing == 3)
			viewport.Msaa3D = Viewport.Msaa.Msaa2X;
		else if (Settings.Graphics.Antialiasing == 4)
			viewport.Msaa3D = Viewport.Msaa.Msaa4X;
		else if (Settings.Graphics.Antialiasing == 5)
			viewport.Msaa3D = Viewport.Msaa.Msaa8X;
		
		DisplayServer.WindowSetVsyncMode((DisplayServer.VSyncMode)Settings.Graphics.Vsync);

		if (Settings.Graphics.WindowMode == 1)
		{
			window.Mode = Window.ModeEnum.Maximized;
		}
		else if (Settings.Graphics.WindowMode == 2)
			window.Mode = Window.ModeEnum.ExclusiveFullscreen;
		else
			window.Mode = Window.ModeEnum.Fullscreen;
		
		// Positional Shadows можно отключить, выставив им atlas size = 0
		viewport.PositionalShadowAtlasSize = Settings.Graphics.ShadowAtlasSize;
		
		// Directional Shadows - нельзя, обходим отдельно
		RenderingServer.DirectionalShadowAtlasSetSize(int.Max(256, Settings.Graphics.ShadowAtlasSize), true);
		GameManager.Instance.DirectionalShadowsEnabled = Settings.Graphics.ShadowAtlasSize != 0;
		
		RenderingServer.PositionalSoftShadowFilterSetQuality((RenderingServer.ShadowQuality)Settings.Graphics.ShadowFilterQuality);
		RenderingServer.DirectionalSoftShadowFilterSetQuality((RenderingServer.ShadowQuality)Settings.Graphics.ShadowFilterQuality);
		TrackManager.Instance.ApplyShadowSettings();

		GameManager.Instance.NotifyViewportSettingsChanged();
	}
}