using Godot;
using System;
using System.Threading;
using racingGame;

public partial class PrefMonitor : Label
{
	public override void _Ready()
	{
		Visible = SettingsManager.Instance.Settings.PerfMonEnabled;
	}
	
	public override void _Process(double delta)
	{
		if (Visible)
		{
			Text = "Frames Per Second: " + Performance.GetMonitor(Performance.Monitor.TimeFps) + "\n" + 
				   "Frame Time: " + (Performance.GetMonitor(Performance.Monitor.TimeProcess)*1000).ToString("0.00") + " ms" + "\n" + 
				   "VRAM Used: " + FormatMemory(Performance.GetMonitor(Performance.Monitor.RenderVideoMemUsed)) + "\n" +
				   "Total Objects: " + Performance.GetMonitor(Performance.Monitor.ObjectCount) + "\n" +
				   "Objects Rendered: " + Performance.GetMonitor(Performance.Monitor.RenderTotalObjectsInFrame) + "\n" +
				   "Draw Calls: " + Performance.GetMonitor(Performance.Monitor.RenderTotalDrawCallsInFrame);
		}
	}

	private string FormatMemory(double bytes)
	{
		return (bytes/(1024*1024)).ToString("0.00") + " MB";	
	}
}
