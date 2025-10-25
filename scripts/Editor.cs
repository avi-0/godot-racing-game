using System;
using System.Collections.Generic;
using System.Linq;
using Fractural.Tasks;
using Godot;

namespace racinggame;

public partial class Editor : Node
{
	public const float CellSize = 8;
	public const float CellHeight = 1;
	public const string BlockPath = "res://blocks/";
	
	[Export] public float CameraSpeed;

	[Export] public EditorViewport EditorViewport;
	
	[Export] public Camera3D Camera;

	[Export] public PackedScene BlockScene;

	[Export] public Material BlockEraseHighlightMaterial;

	[Export] public Material BlockHighlightMaterial;

	[Export] public Container BlockListContainer;

	[Export] public PopupMenu FileMenu;

	[Export] public FileDialog FileDialog;

	[Export] public Button PlayButton;

	[Export] public Control EditorUINode;

	private Node3D TrackNode => GameManager.Singleton.TrackNode;
	
	private Block Cursor;

	private Block _hoveredBlock;

	private int YLevel = 0;

	private int _rotation = 0;

	private bool _isRunning = true;
	
	public bool IsRunning
	{
		get => _isRunning;
		set
		{
			ProcessMode = value ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
			EditorUINode.Visible = value;
			
			if (value)
			{
				CreateCursor();
			}
			else
			{
				DestroyCursor();
			}
			
			_isRunning = value;
		}
	}

	private enum Mode
	{
		Normal,
		Erase,
	}

	private Mode _mode = Mode.Normal;
	
	public override void _Ready()
	{
		PlayButton.Pressed += PlayButtonOnPressed;
		EditorViewport.Input += ViewportInput;
		
		FileMenu.IdPressed += FileMenuOnIdPressed;
		FileMenu.SetItemAccelerator(0, (Key) KeyModifierMask.MaskCtrl | Key.O);
		FileMenu.SetItemAccelerator(1, (Key) KeyModifierMask.MaskCtrl | Key.S);
		
		FileDialog.FileSelected += FileDialogOnFileSelected;

		DirAccess.MakeDirRecursiveAbsolute("user://tracks/");
		
		CreateCursor();
		
		UpdateBlockList();
	}

	private void FileMenuOnIdPressed(long id)
	{
		if (id == 0)
		{
			// open
			FileDialog.FileMode = FileDialog.FileModeEnum.OpenFile;
			FileDialog.Visible = true;
		}
		else if (id == 1)
		{
			// save
			FileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
			FileDialog.Visible = true;
		}
	}
	
	private void FileDialogOnFileSelected(string path)
	{
		if (FileDialog.FileMode == FileDialog.FileModeEnum.OpenFile)
		{
			GameManager.Singleton.OpenTrack(path);

			foreach (var block in TrackNode.FindChildren("*", "Block").Cast<Block>())
			{
				ConnectBlockSignals(block);
			}
		}
		else if (FileDialog.FileMode == FileDialog.FileModeEnum.SaveFile)
		{
			GameManager.Singleton.SaveTrack(path);
		}
	}

	private void PlayButtonOnPressed()
	{
		PlayButtonOnPressedAsync().Forget();
	}

	private async GDTaskVoid PlayButtonOnPressedAsync()
	{
		IsRunning = false;
		
		GameManager.Singleton.Play();
		await GDTask.ToSignal(GameManager.Singleton, GameManager.SignalName.StoppedPlaying);

		IsRunning = true;
	}

	private IEnumerable<String> GetBlockPaths(string basePath, string dirPath = "")
	{
		foreach (var path in ResourceLoader.ListDirectory(basePath + dirPath).ToList().Order())
		{
			var subpath = dirPath.PathJoin(path);
			if (ResourceLoader.Exists(basePath.PathJoin(subpath), "BlockRecord"))
				yield return subpath;

			foreach (var result in GetBlockPaths(basePath, subpath))
				yield return result;
		}
	}

	private IEnumerable<BlockRecord> GetBlockRecords()
	{
		return GetBlockPaths(BlockPath)
			.Order()
			.Select(path => ResourceLoader.Load(BlockPath.PathJoin(path), "BlockRecord"))
			// fuck knows why the type hint doesn't work right
			.Where(resource => resource is BlockRecord)
			.Cast<BlockRecord>();
	}

	public override void _Process(double delta)
	{
		_mode = Mode.Normal;
		if (Input.IsActionPressed("editor_erase"))
			_mode = Mode.Erase;
		
		UpdateCamera((float) delta);

		Cursor.GlobalPosition = GetGridMousePosition();
		Cursor.Visible = true;

		if (_hoveredBlock != null && !IsInstanceValid(_hoveredBlock))
		{
			_hoveredBlock = null;
		}
		
		if (_mode == Mode.Erase)
		{
			Cursor.Visible = false;
			
			if (_hoveredBlock != null)
				_hoveredBlock.SetMaterialOverlay(BlockEraseHighlightMaterial);
		}

		if (_mode == Mode.Normal)
		{
			if (_hoveredBlock != null)
				_hoveredBlock.SetMaterialOverlay(null);
		}
	}

	private void CreateCursor()
	{
		Cursor?.QueueFree();

		Cursor = BlockScene.Instantiate<Block>();
		
		AddChild(Cursor);
		Cursor.RotateY(-float.DegreesToRadians(90) * _rotation);
		Cursor.SetMaterialOverlay(BlockHighlightMaterial);
	}

	private void DestroyCursor()
	{
		if (Cursor != null)
		{
			RemoveChild(Cursor);
			Cursor.QueueFree();
			Cursor = null;
		}
	}

	private void PlaceCursorBlock()
	{
		if (Input.IsKeyPressed(Key.Shift))
		{
			var existingBlock = GetBlockAtPosition(Cursor.GlobalPosition);
			if (existingBlock != null)
			{
				EraseBlock(existingBlock);
			}
		}
		
		Cursor.SetMaterialOverlay(null);
		Cursor.Reparent(TrackNode, true);
		ConnectBlockSignals(Cursor);
		
		UiSoundPlayer.__Instance.PlayBlockPlaced();

		Cursor = null;
		CreateCursor();
	}

	private void ConnectBlockSignals(Block block)
	{
		block.ChildMouseEntered += OnBlockMouseEntered;
	}

	private void OnBlockMouseEntered(Block block)
	{
		if (!IsRunning)
			return;
		
		if (_hoveredBlock != null)
			_hoveredBlock.SetMaterialOverlay(null);
		
		block.SetMaterialOverlay(BlockEraseHighlightMaterial);
		_hoveredBlock = block;
	}

	private void RotateCursor()
	{
		Cursor.RotateY(-float.DegreesToRadians(90));
		Cursor.GlobalRotationDegrees = Cursor.GlobalRotationDegrees.Round();
		
		_rotation = (_rotation + 1) % 4;
	}

	private void UpdateCamera(float delta)
	{
		if (Input.IsActionPressed("editor_left"))
			Camera.GlobalPosition += delta * CameraSpeed * Vector3.Left;
		if (Input.IsActionPressed("editor_right"))
			Camera.GlobalPosition += delta * CameraSpeed * Vector3.Right;
		if (Input.IsActionPressed("editor_forward"))
			Camera.GlobalPosition += delta * CameraSpeed * Vector3.Forward;
		if (Input.IsActionPressed("editor_back"))
			Camera.GlobalPosition += delta * CameraSpeed * Vector3.Back;
		if (Input.IsActionPressed("editor_up"))
			Camera.GlobalPosition += delta * CameraSpeed * Vector3.Up;
		if (Input.IsActionPressed("editor_down"))
			Camera.GlobalPosition += delta * CameraSpeed * Vector3.Down;
	}

	private Vector2 ProjectMousePosition()
	{
		var plane = new Plane(Vector3.Up, new Vector3(0, YLevel * CellHeight, 0));
		var mousePosition = EditorViewport.GetMousePosition();
		var from = Camera.ProjectRayOrigin(mousePosition);
		var dir = Camera.ProjectRayNormal(mousePosition);
		var intersection = plane.IntersectsRay(from, dir) ?? Vector3.Zero;
		
		return new Vector2(intersection.X, intersection.Z);
	}

	private Vector3 GetGridMousePosition()
	{
		var pos = ProjectMousePosition();
		return new Vector3(
			CellSize * (float.Round(pos.X / CellSize - 0.5f) + 0.5f),
			YLevel * CellHeight,
			CellSize * (float.Round(pos.Y / CellSize - 0.5f) + 0.5f));
	}

	public void ViewportInput(InputEvent @event)
	{
		if (!IsRunning)
			return;
		
		if (_mode == Mode.Normal)
		{
			if (@event is InputEventMouseButton mouseEvent)
			{
				if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.IsPressed())
				{
					PlaceCursorBlock();
				}

				if (mouseEvent.ButtonIndex == MouseButton.Right && mouseEvent.IsPressed())
				{
					RotateCursor();
				}

				if (mouseEvent.ButtonIndex == MouseButton.WheelDown)
				{
					YLevel--;
					Camera.GlobalPosition += CellHeight * Vector3.Down;
				}

				if (mouseEvent.ButtonIndex == MouseButton.WheelUp)
				{
					YLevel++;
					Camera.GlobalPosition += CellHeight * Vector3.Up;
				}
			}
		}

		if (_mode == Mode.Erase)
		{
			if (@event is InputEventMouseButton mouseEvent)
			{
				if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.IsPressed())
				{
					EraseHoveredBlock();
				}
			}
		}
	}

	private void EraseBlock(Block block)
	{
		TrackNode.RemoveChild(block);
		block.QueueFree();

		if (_hoveredBlock == block)
			_hoveredBlock = null;
	}

	private void EraseHoveredBlock()
	{
		if (_hoveredBlock != null)
		{
			EraseBlock(_hoveredBlock);
			
			UiSoundPlayer.__Instance.PlayBlockPlaced(0.8f);
		}
	}

	private Block GetBlockAtPosition(Vector3 pos)
	{
		foreach (var block in TrackNode.GetChildren().Cast<Block>())
		{
			if (block.GlobalPosition.IsEqualApprox(pos))
			{
				return block;
			}
		}

		return null;
	}

	private void UpdateBlockList()
	{
		foreach (var child in BlockListContainer.GetChildren())
			child.QueueFree();
		
		foreach (var record in GetBlockRecords())
		{
			var button = new Button();
			button.CustomMinimumSize = 64 * Vector2.One;
			button.Icon = record.ThumbnailTexture;
			button.IconAlignment = HorizontalAlignment.Center;
			button.ExpandIcon = true;
			button.TooltipText = record.ResourcePath;

			BlockListContainer.AddChild(button);
			
			button.Pressed += () => OnBlockButtonPressed(record);
		}
	}

	private void OnBlockButtonPressed(BlockRecord blockRecord)
	{
		BlockScene = blockRecord.Scene;
		
		CreateCursor();
	}
}
