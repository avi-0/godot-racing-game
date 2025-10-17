using System;
using System.Collections.Generic;
using System.Linq;
using Fractural.Tasks;
using Godot;
using racingGame.blocks;

namespace racingGame;

public partial class Editor : Node
{
	public const float CellSize = 8;
	public const float CellHeight = 2;
	public const string BlockPath = "res://blocks/";
	
	[Export] public float CameraSpeed;
	
	[Export] public Camera3D Camera;

	[Export] public PackedScene BlockScene;

	[Export] public Node3D TrackBlocksNode;

	[Export] public Material BlockEraseHighlightMaterial;

	[Export] public VBoxContainer BlockListContainer;

	[Export] public Button PlayButton;

	[Export] public Control EditorUINode;

	private blocks.Block Cursor;

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
		
		CreateCursor();
		
		UpdateBlockList();
	}

	private void PlayButtonOnPressed()
	{
		PlayButtonOnPressedAsync().Forget();
	}

	private async GDTaskVoid PlayButtonOnPressedAsync()
	{
		IsRunning = false;
		
		GameManager.__Instance.Play();
		await GDTask.ToSignal(GameManager.__Instance, GameManager.SignalName.StoppedPlaying);

		IsRunning = true;
	}

	private IEnumerable<String> GetBlockPaths(string basePath, string dirPath = "")
	{
		foreach (var path in ResourceLoader.ListDirectory(basePath + dirPath).ToList().Order())
		{
			var subpath = dirPath + path;
			if (ResourceLoader.Exists(basePath + subpath, "PackedScene"))
				yield return subpath;

			foreach (var result in GetBlockPaths(basePath, subpath))
				yield return result;
		}
	}

	public override void _Process(double delta)
	{
		_mode = Mode.Normal;
		if (Input.IsActionPressed("editor_erase"))
			_mode = Mode.Erase;
		
		UpdateCamera((float) delta);

		Cursor.GlobalPosition = GetGridMousePosition();
		Cursor.Visible = true;
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

		Cursor = BlockScene.Instantiate<blocks.Block>();
		Cursor.PhysicsInterpolationMode = PhysicsInterpolationModeEnum.Off;
		AddChild(Cursor);
		Cursor.RotateY(-float.DegreesToRadians(90) * _rotation);
	}

	private void PlaceCursorBlock()
	{
		var existingBlock = GetBlockAtPosition(Cursor.GlobalPosition);
		if (existingBlock != null)
		{
			EraseBlock(existingBlock);
		}
		
		Cursor.Reparent(TrackBlocksNode, true);
		Cursor.ChildMouseEntered += OnBlockMouseEntered;
		
		UiSoundPlayer.__Instance.PlayBlockPlaced();

		Cursor = null;
		CreateCursor();
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
		var mousePosition = GetViewport().GetMousePosition();
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

	public override void _UnhandledInput(InputEvent @event)
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
		TrackBlocksNode.RemoveChild(block);
		block.QueueFree();

		if (_hoveredBlock == block)
			_hoveredBlock = null;
	}

	private void EraseHoveredBlock()
	{
		if (_hoveredBlock != null)
		{
			EraseBlock(_hoveredBlock);
		}
	}

	private Block GetBlockAtPosition(Vector3 pos)
	{
		foreach (var block in TrackBlocksNode.GetChildren().Cast<Block>())
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
		
		foreach (var path in GetBlockPaths(BlockPath))
		{
			var button = new Button();
			button.Text = path;
			button.Alignment = HorizontalAlignment.Left;
			button.AutowrapMode = TextServer.AutowrapMode.WordSmart;

			BlockListContainer.AddChild(button);
			
			button.Pressed += () => OnBlockButtonPressed(BlockPath + path);
		}
	}

	private void OnBlockButtonPressed(string path)
	{
		BlockScene = ResourceLoader.Load<PackedScene>(path);
		
		CreateCursor();
	}
}
