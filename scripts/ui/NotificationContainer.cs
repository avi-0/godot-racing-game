using Godot;
using System;

public partial class NotificationContainer : VBoxContainer
{
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void DisplayMessage(string message, int duration = 30)
	{
		Panel panel = new Panel();
		panel.CustomMinimumSize = new Vector2(GetSize().X, 64 + (int)Math.Round((double)message.Length/2.5));
		panel.SetSize(panel.CustomMinimumSize);
		
		RichTextLabel messageLabel = new RichTextLabel();
		messageLabel.Text = message;
		messageLabel.CustomMinimumSize = (panel.CustomMinimumSize);
		messageLabel.SetSize(panel.CustomMinimumSize);
		messageLabel.SetUseBbcode(true);
		messageLabel.SetHorizontalAlignment(HorizontalAlignment.Center);
		messageLabel.SetVerticalAlignment(VerticalAlignment.Center);	
		panel.AddChild(messageLabel);
		
		AddChild(panel);
		
		Timer timer = new Timer();
		timer.SetWaitTime(duration);
		timer.Timeout += () => { RemoveChild(panel); panel.Dispose(); };
		panel.AddChild(timer);
		timer.Start();
	}

	public void Clear()
	{
		foreach (Node child in GetChildren())
		{
			RemoveChild(child);
			child.Dispose();
		}
	}
}
