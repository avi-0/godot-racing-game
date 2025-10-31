using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Fractural.Tasks;
using Godot;

namespace racingGame;

public partial class Editor : Control
{
	public const string BlockPath = "res://blocks/";

	public static Editor Singleton;

	[Export] public float CameraSpeed;

	[Export] public EditorViewport EditorViewport;
	
	[Export] public Camera3D Camera;

	[Export] public PackedScene BlockScene;

	[Export] public Material BlockEraseHighlightMaterial;

	[Export] public Material BlockHighlightMaterial;

	[Export] public GridContainer BlockListContainer;

	[Export] public PopupMenu FileMenu;

	[Export] public FileDialog FileDialog;

	[Export] public ConfirmationDialog ConfirmNewDialog;
	
	[Export] public ConfirmationDialog ConfirmQuitDialog;

	[Export] public Button PlayButton;

	[Export] public Control CarSelect;

	[Export] public Container CarSelectContainer;

	[Export] public Control EditorUINode;

	[Export] public LineEdit GridSizeLabel;

	[Export] public Button GridSizeDecButton;
	
	[Export] public Button GridSizeIncButton;

	[Export] public PackedScene EditorBlockButtonScene;

	[Export] public HSplitContainer HSplitContainer;

	[Export] public Container DirectoryListContainer;
	
	[Signal]
	public delegate void ExitedEventHandler();

	private Node3D TrackNode => GameManager.Singleton.TrackNode;
	
	private Block _cursor;

	private Block _hoveredBlock;

	private float _yLevel = 0;

	private float YLevelRounded => _cellHeight * float.Round(_yLevel / _cellHeight);

	private int _rotation = 0;

	private bool _isRunning = true;

	private int _gridSizeSetting;
	private float _cellSize = 8;
	private float _cellHeight = 1;

	private string _blockDirectory;
	
	public bool IsRunning
	{
		get => _isRunning;
		set
		{
			ProcessMode = value ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
			Visible = value;
			
			CarSelect.Visible = false;

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
		Singleton = this;

		PlayButton.Pressed += PlayButtonOnPressed;
		EditorViewport.Input += ViewportInput;
		
		FileMenu.IdPressed += FileMenuOnIdPressed;
		FileMenu.SetItemAccelerator(FileMenu.GetItemIndex(0), (Key) KeyModifierMask.MaskCtrl | Key.O);
		FileMenu.SetItemAccelerator(FileMenu.GetItemIndex(1), (Key) KeyModifierMask.MaskCtrl | Key.S);
		FileMenu.SetItemAccelerator(FileMenu.GetItemIndex(3), Key.Escape);
		
		ConfirmNewDialog.Confirmed += ConfirmNewDialogOnConfirmed;
		ConfirmQuitDialog.Confirmed += ConfirmQuitDialogOnConfirmed;
		FileDialog.FileSelected += FileDialogOnFileSelected;

		SetGridSizeSetting(3);
		GridSizeDecButton.Pressed += () => { SetGridSizeSetting(_gridSizeSetting - 1); };
		GridSizeIncButton.Pressed += () => { SetGridSizeSetting(_gridSizeSetting + 1); };
		
		HSplitContainer.Dragged += HSplitContainerOnDragged;

		DirAccess.MakeDirRecursiveAbsolute("user://tracks/");
		
		CreateCursor();
		
		SetDirectory("/");
	
		LoadCarList();
	}

	private void HSplitContainerOnDragged(long offset)
	{
		var width = BlockListContainer.Size.X;
		int itemWidth = 64;
		var sep = BlockListContainer.GetThemeConstant("separation");
		var columns = (int) Math.Floor((width + sep) / (itemWidth + sep));
		BlockListContainer.Columns = columns;
	}

	private void SetGridSizeSetting(int setting)
	{
		setting = int.Clamp(setting, -3, 3);
		_gridSizeSetting = setting;
		_cellSize = Mathf.Pow(2, setting);
		_cellHeight = float.Min(_cellSize, 2f);
		GridSizeLabel.Text = _cellSize.ToString();
	}

	private void ConfirmNewDialogOnConfirmed()
	{
		GameManager.Singleton.NewTrack();
	}

	private void FileMenuOnIdPressed(long id)
	{
		if (id == 0)
		{
			// open
			FileDialog.FileMode = FileDialog.FileModeEnum.OpenFile;
			FileDialog.Show();
		}
		else if (id == 1)
		{
			// save
			FileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
			FileDialog.Show();
		}
		else if (id == 2)
		{
			ConfirmNewDialog.Show();
		}
		else if (id == 3)
		{
			ConfirmQuitDialog.Show();
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
		CarSelect.Visible = true;
	}

	private async GDTaskVoid PlayButtonOnPressedAsync()
	{
		IsRunning = false;

		GameModeController.CurrentGameMode.InitTrack(TrackNode);
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
		}
	}

	private IEnumerable<BlockRecord> GetBlockRecords(string path)
	{
		return GetBlockPaths(BlockPath, path)
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

		_cursor.GlobalPosition = GetGridMousePosition();
		_cursor.Visible = true;

		if (_hoveredBlock != null && !IsInstanceValid(_hoveredBlock))
		{
			_hoveredBlock = null;
		}
		
		if (_mode == Mode.Erase)
		{
			_cursor.Visible = false;
			
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
		_cursor?.QueueFree();

		_cursor = BlockScene.Instantiate<Block>();
		
		AddChild(_cursor);
		_cursor.RotateY(-float.DegreesToRadians(90) * _rotation);
		_cursor.SetMaterialOverlay(BlockHighlightMaterial);
	}

	private void DestroyCursor()
	{
		if (_cursor != null)
		{
			RemoveChild(_cursor);
			_cursor.QueueFree();
			_cursor = null;
		}
	}

	private void PlaceCursorBlock()
	{
		if (Input.IsKeyPressed(Key.Shift))
		{
			var existingBlock = GetBlockAtPosition(_cursor.GlobalPosition);
			if (existingBlock != null)
			{
				EraseBlock(existingBlock);
			}
		}
		
		_cursor.SetMaterialOverlay(null);
		_cursor.Reparent(TrackNode, true);
		_cursor.Owner = TrackNode;
		ConnectBlockSignals(_cursor);
		
		UiSoundPlayer.__Instance.PlayBlockPlaced();

		_cursor = null;
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
		_cursor.RotateY(-float.DegreesToRadians(90));
		_cursor.GlobalRotationDegrees = _cursor.GlobalRotationDegrees.Round();
		
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
		var plane = new Plane(Vector3.Up, new Vector3(0, YLevelRounded, 0));
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
			_cellSize * float.Round(pos.X / _cellSize),
			YLevelRounded,
			_cellSize * float.Round(pos.Y / _cellSize));
	}

	public void ViewportInput(InputEvent @event)
	{
		if (!IsRunning)
			return;
		
		if (_mode == Mode.Normal)
		{
			if (@event is InputEventMouseButton mouseEvent && mouseEvent.IsPressed())
			{
				if (mouseEvent.ButtonIndex == MouseButton.Left)
				{
					PlaceCursorBlock();
				}

				if (mouseEvent.ButtonIndex == MouseButton.Right)
				{
					RotateCursor();
				}

				if (mouseEvent.ButtonIndex == MouseButton.WheelDown)
				{
					_yLevel -= _cellHeight;
					Camera.GlobalPosition += _cellHeight * Vector3.Down;
				}

				if (mouseEvent.ButtonIndex == MouseButton.WheelUp)
				{
					_yLevel += _cellHeight;
					Camera.GlobalPosition += _cellHeight * Vector3.Up;
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
	
	private void OnBlockButtonPressed(BlockRecord blockRecord)
	{
		BlockScene = blockRecord.Scene;
		
		CreateCursor();
	}

	private void LoadCarList()
	{
		var paths = GameManager.Singleton.LoadCarList();

		GD.Print($"{paths.GetType()}");

		foreach (var carPath in paths)
		{
			var button = new Button();
			button.CustomMinimumSize = 64 * Vector2.One;
			button.Text = carPath;
			button.Pressed += () => OnCarButtonPressed(carPath);

			CarSelectContainer.AddChild(button);
		}
	}

	private void OnCarButtonPressed(string carPath)
	{
		CarSelect.Visible = false;
		GameManager.Singleton.SelectCarScene(carPath);
		PlayButtonOnPressedAsync().Forget();
	}
	
	private void ConfirmQuitDialogOnConfirmed()
	{
		IsRunning = false;
		EmitSignalExited();
	}

	private void SetDirectory(string path)
	{
		_blockDirectory = path;
		
		DirectoryListContainer.DestroyAllChildren();
		BlockListContainer.DestroyAllChildren();

		var dir = DirAccess.Open(BlockPath.PathJoin(_blockDirectory));

		if (path != "/")
		{
			var baseDir = path.GetBaseDir();
			
			var button = new Button();
			button.Text = "..";
			button.Pressed += () => SetDirectory(baseDir);

			DirectoryListContainer.AddChild(button);
		}
		
		foreach (var subDir in dir.GetDirectories())
		{
			if (subDir.GetFile().StartsWith('_'))
				continue;
			
			var subDirPath = _blockDirectory.PathJoin(subDir);

			var button = new Button();
			button.Text = subDir;
			button.Pressed += () => SetDirectory(subDirPath);
			
			DirectoryListContainer.AddChild(button);
		}
		
		foreach (var record in GetBlockRecords(path))
		{
			var button = EditorBlockButtonScene.Instantiate<Button>();
			button.Icon = record.ThumbnailTexture;
			button.TooltipText = record.ResourcePath;

			BlockListContainer.AddChild(button);
			
			button.Pressed += () => OnBlockButtonPressed(record);
		}
	}
}
