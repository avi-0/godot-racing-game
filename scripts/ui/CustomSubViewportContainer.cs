using System.Collections.Generic;
using Godot;

namespace racingGame;

[GlobalClass]
[Tool]
public partial class CustomSubViewportContainer : Container
{
	// получен из ванильного SubViewportContainer
	// конвертирован из C++ в C# с помощью Claude
	// нужен чтобы сохранять разрешение Viewport'а при различных масштабах
	// - по какой-то причине SubViewportContainer сделан так, что выбить из него нужное поведение
	// совершенно нереально (я пытался, было больно)
	// https://github.com/godotengine/godot/issues/77149
	// https://github.com/godotengine/godot-proposals/issues/11680 🙏 inshallah they fix it 🙏
	// см. RecalcForceViewportSizes
	private bool _stretch = true;
	private int _shrink = 1;
	private bool _mouseTarget = false;
	
	[Export]
	public bool Stretch
	{
		get => _stretch;
		set => SetStretch(value);
	}
	
	[Export(PropertyHint.Range, "1,32,1,or_greater")]
	public int StretchShrink
	{
		get => _shrink;
		set => SetStretchShrink(value);
	}
	
	[Export]
	public bool MouseTarget
	{
		get => _mouseTarget;
		set => SetMouseTarget(value);
	}

	public CustomSubViewportContainer()
	{
		SetProcessUnhandledInput(true);
		FocusMode = FocusModeEnum.Click;
	}

	public override Vector2 _GetMinimumSize()
	{
		if (_stretch)
		{
			return Vector2.Zero;
		}

		Vector2 ms = Vector2.Zero;
		foreach (Node child in GetChildren())
		{
			if (child is SubViewport subViewport)
			{
				Vector2 minSize = subViewport.Size;
				ms = ms.Max(minSize);
			}
		}

		return ms;
	}

	public void SetStretch(bool enable)
	{
		if (_stretch == enable)
		{
			return;
		}

		_stretch = enable;
		RecalcForceViewportSizes();
		UpdateMinimumSize();
		QueueSort();
		QueueRedraw();
	}

	public bool IsStretchEnabled()
	{
		return _stretch;
	}

	public void SetStretchShrink(int shrink)
	{
		if (shrink < 1)
		{
			GD.PushError("Stretch shrink must be at least 1");
			return;
		}

		if (_shrink == shrink)
		{
			return;
		}

		_shrink = shrink;
		RecalcForceViewportSizes();
		QueueRedraw();
	}

	public int GetStretchShrink()
	{
		return _shrink;
	}

	public void RecalcForceViewportSizes()
	{
		if (!_stretch)
		{
			return;
		}
		
		// изменения здесь
		var scale = GetViewport()?.GetStretchTransform().Scale ?? Vector2.One;
		var size = (Vector2I) (Size * scale / _shrink).Round();
		
		foreach (Node child in GetChildren())
		{
			if (child is SubViewport subViewport)
			{
				subViewport.Size = size;
			}
		}
	}

	public void SetMouseTarget(bool enable)
	{
		_mouseTarget = enable;
	}

	public bool IsMouseTargetEnabled()
	{
		return _mouseTarget;
	}

	public override string[] _GetConfigurationWarnings()
	{
		var warnings = new List<string>();

		bool hasViewport = false;
		foreach (Node child in GetChildren())
		{
			if (child is SubViewport)
			{
				hasViewport = true;
				break;
			}
		}

		if (!hasViewport)
		{
			warnings.Add("This node doesn't have a SubViewport as child, so it can't display its intended content.\n" +
						"Consider adding a SubViewport as a child to provide something displayable.");
		}

		if (GetDefaultCursorShape() != CursorShape.Arrow)
		{
			warnings.Add("The default mouse cursor shape of SubViewportContainer has no effect.\n" +
						"Consider leaving it at its initial value `CURSOR_ARROW`.");
		}

		return warnings.ToArray();
	}

	public override void _Notification(int what)
	{
		base._Notification(what);

		switch ((long)what)
		{
			case NotificationResized:
				RecalcForceViewportSizes();
				break;

			case NotificationEnterTree:
			case NotificationVisibilityChanged:
				foreach (Node child in GetChildren())
				{
					if (child is SubViewport subViewport)
					{
						if (IsVisibleInTree())
						{
							subViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
						}
						else
						{
							subViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Disabled;
						}

						subViewport.HandleInputLocally = false; // do not handle input locally here
					}
				}
				break;

			case NotificationDraw:
				foreach (Node child in GetChildren())
				{
					if (child is SubViewport subViewport)
					{
						if (_stretch)
						{
							DrawTextureRect(subViewport.GetTexture(), new Rect2(Vector2.Zero, Size), false);
						}
						else
						{
							DrawTextureRect(subViewport.GetTexture(), new Rect2(Vector2.Zero, subViewport.Size), false);
						}
					}
				}
				break;

			case NotificationFocusEnter:
				// If focused, send InputEvent to the SubViewport before the Gui-Input stage.
				SetProcessInput(true);
				SetProcessUnhandledInput(false);
				break;

			case NotificationFocusExit:
				// A different Control has focus and should receive Gui-Input before the InputEvent is sent to the SubViewport.
				SetProcessInput(false);
				SetProcessUnhandledInput(true);
				break;
		}
	}

	public override void _Input(InputEvent @event)
	{
		PropagateNonpositionalEvent(@event);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		PropagateNonpositionalEvent(@event);
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event == null)
		{
			return;
		}

		if (Engine.IsEditorHint())
		{
			return;
		}

		if (!IsEventPropagatedInGuiInput(@event))
		{
			return;
		}

		if (!ShouldPropagateInputEvent(@event))
		{
			return;
		}

		if (_stretch && _shrink > 1)
		{
			Transform2D xform = Transform2D.Identity;
			xform = xform.Scaled(Vector2.One / _shrink);
			SendEventToViewports(@event.XformedBy(xform));
		}
		else
		{
			SendEventToViewports(@event);
		}
	}

	private void PropagateNonpositionalEvent(InputEvent @event)
	{
		if (@event == null)
		{
			return;
		}

		if (Engine.IsEditorHint())
		{
			return;
		}

		if (IsEventPropagatedInGuiInput(@event))
		{
			return;
		}

		if (!ShouldPropagateInputEvent(@event))
		{
			return;
		}

		SendEventToViewports(@event);
	}
	
	protected virtual bool ShouldPropagateInputEvent(InputEvent @event)
	{
		// Default behavior: propagate all events
		return true;
	}

	private void SendEventToViewports(InputEvent @event)
	{
		foreach (Node child in GetChildren())
		{
			if (child is SubViewport subViewport && !subViewport.GuiDisableInput)
			{
				subViewport.PushInput(@event);
			}
		}
	}

	private bool IsEventPropagatedInGuiInput(InputEvent @event)
	{
		// Propagation of events with a position property happen in gui_input
		// Propagation of other events happen in input
		return @event is InputEventMouse or InputEventScreenDrag or InputEventScreenTouch or InputEventGesture;
	}
}