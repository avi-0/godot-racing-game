using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Fractural.Tasks;
using Godot;

namespace racingGame;

public partial class Editor : Control
{
	[Signal]
	public delegate void ExitedEventHandler();

	public const string BlockPath = "res://blocks/";

	public static Editor Singleton;

	private string _blockDirectory;
	private Transform3D _grid = new(Basis.Identity, Vector3.Zero);
	private float _gridScale = 8.0f;
	private float _gridHeightScale = 1.0f;

	private Block _cursor;

	private int _gridSizeSetting;

	private Block _hoveredBlock;

	private bool _isRunning = true;
	
	private enum Mode
	{
		Normal,
		Erase,
		Pick,
		GridPick,
	}

	private Mode _mode = Mode.Normal;

	private int _rotation = 0;

	private float _yLevel = 0;

	private bool _mouseOverViewport = false;

	[Export] public Material BlockEraseHighlightMaterial;

	[Export] public Material BlockHighlightMaterial;

	[Export] public GridContainer BlockListContainer;

	[Export] public BlockRecord CurrentBlockRecord;

	[Export] public OrbitCamera Camera;

	[Export] public ConfirmationDialog ConfirmNewDialog;

	[Export] public ConfirmationDialog ConfirmQuitDialog;

	[Export] public Container DirectoryListContainer;

	[Export] public PackedScene EditorBlockButtonScene;
	
	[Export]
	public SubViewportContainer EditorViewportContainer;

	[Export] public EditorViewport EditorViewport;

	[Export]
	public Button EraseButton;
	
	[Export]
	public Button PickButton;
	
	[Export]
	public Button GridPickButton;
	
	[Export]
	public Button ResetGridButton;
	
	[Export]
	public OptionButton RotationStepButton;
	
	[Export]
	public Button YawPlusButton;
	
	[Export]
	public Button YawMinusButton;
	
	[Export]
	public Button PitchPlusButton;
	
	[Export]
	public Button PitchMinusButton;
	
	[Export]
	public Button RollPlusButton;
	
	[Export]
	public Button RollMinusButton;
	
	[Export]
	public Button ResetRotationButton;

	[Export] public FileDialog FileDialog;

	[Export] public Button GridSizeDecButton;

	[Export] public Button GridSizeIncButton;

	[Export] public LineEdit GridSizeLabel;

	[Export] public HSplitContainer HSplitContainer;

	[Export] public Button OpenButton;

	[Export] public Tree OptionsTree;

	[Export] public Button PlayButton;

	[Export] public Button QuitButton;

	[Export] public Button SaveButton;
	
	[Export]
	public Button SetThumbnailButton;

	[Export]
	public Control ScreenshotUi;
	
	[Export]
	public SubViewportContainer ScreenshotViewportContainer;

	[Export]
	public SubViewport ScreenshotViewport;

	[Export]
	public FreeCamera ScreenshotCamera;

	[Export]
	public Button ThumbnailConfirmButton;
	
	[Export]
	public Button ThumbnailCancelButton;

	[Export]
	public TextureRect ThumbnailTextureRect;

	private bool _takingScreenshot = false;
	
	const int GridMeshSize = 100;
	
	[Export]
	public MeshInstance3D GridMeshInstance;

	[Export]
	public ImmediateMesh GridMesh;

	private float _rotationStep = float.DegreesToRadians(5);
	private bool _rotationShiftOrigin = false;

	private Track Track => GameManager.Singleton.Track;

	public bool IsRunning
	{
		get => _isRunning;
		set
		{
			ProcessMode = value ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
			
			GridMeshInstance.Visible = value;
			Visible = value;

			if (Visible)
			{
				EditorViewport.MatchViewport(GetViewport(), aa: false);
			}

			if (value)
				CreateCursor();
			else
				DestroyCursor();

			_isRunning = value;
		}
	}

	public override void _Ready()
	{
		Singleton = this;

		EditorViewport.Input += ViewportInput;
		EditorViewportContainer.MouseEntered += () => _mouseOverViewport = true;
		EditorViewportContainer.MouseExited += () => _mouseOverViewport = false;
		
		Camera.Pitch = float.DegreesToRadians(45);
		Camera.Yaw = float.DegreesToRadians(180);
		Camera.Radius = 48;

		QuitButton.Pressed += () => ConfirmQuitDialog.Show();
		OpenButton.Pressed += () =>
		{
			FileDialog.FileMode = FileDialog.FileModeEnum.OpenFile;
			FileDialog.Show();
		};
		SaveButton.Pressed += () =>
		{
			FileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
			FileDialog.Show();
		};
		PlayButton.Pressed += PlayButtonOnPressed;

		ConfirmNewDialog.Confirmed += ConfirmNewDialogOnConfirmed;
		ConfirmQuitDialog.Confirmed += ConfirmQuitDialogOnConfirmed;
		FileDialog.FileSelected += (path) => FileDialogOnFileSelected(path).Forget();

		EraseButton.Toggled += on => { _mode = on ? Mode.Erase : Mode.Normal; };
		PickButton.Toggled += on => { _mode = on ? Mode.Pick : Mode.Normal; };
		GridPickButton.Toggled += on => { _mode = on ? Mode.GridPick : Mode.Normal; };
		ResetGridButton.Pressed += () => _grid = Transform3D.Identity;
		YawPlusButton.Pressed += YawPlusButtonOnPressed;
		YawMinusButton.Pressed += YawMinusButtonOnPressed;
		PitchPlusButton.Pressed += PitchPlusButtonOnPressed;
		PitchMinusButton.Pressed += PitchMinusButtonOnPressed;
		RollPlusButton.Pressed += RollPlusButtonOnPressed;
		RollMinusButton.Pressed += RollMinusButtonOnPressed;
		ResetRotationButton.Pressed += () => _cursor.Basis = Basis.Identity;
		RotationStepButton.ItemSelected += RotationStepButtonOnItemSelected;

		SetGridSizeSetting(3);
		GridSizeDecButton.Pressed += () => { SetGridSizeSetting(_gridSizeSetting - 1); };
		GridSizeIncButton.Pressed += () => { SetGridSizeSetting(_gridSizeSetting + 1); };

		HSplitContainer.Dragged += HSplitContainerOnDragged;
		
		SetThumbnailButton.Pressed += SetThumbnailButtonOnPressed;
		ThumbnailConfirmButton.Pressed += () => ThumbnailConfirmButtonOnPressed().Forget();
		ThumbnailCancelButton.Pressed += ThumbnailCancelButtonOnPressed;

		DirAccess.MakeDirRecursiveAbsolute("user://tracks/");

		CreateCursor();

		SetDirectory("/").Forget();

		OptionsTree.ItemEdited += OptionEdited;
	}

	private void RotationStepButtonOnItemSelected(long index)
	{
		if (index == 0)
		{
			_rotationStep = float.DegreesToRadians(5);
			_rotationShiftOrigin = false;
		} 
		else if (index == 1)
		{
			_rotationStep = float.Atan(2.0f / 8.0f);
			_rotationShiftOrigin = true;
		}
		else if (index == 2)
		{
			_rotationStep = float.Atan(4.0f / 8.0f);
			_rotationShiftOrigin = true;
		}
	}

	private void RollMinusButtonOnPressed()
	{
		RotateCursor(Vector3.Forward, -_rotationStep, origin: Vector3.Left * 4);
	}

	private void RollPlusButtonOnPressed()
	{
		RotateCursor(Vector3.Forward, _rotationStep, origin: Vector3.Left * 4);
	}

	private void PitchMinusButtonOnPressed()
	{
		RotateCursor(Vector3.Right, -_rotationStep, origin: Vector3.Back * 4);
	}

	private void PitchPlusButtonOnPressed()
	{
		RotateCursor(Vector3.Right, _rotationStep, origin: Vector3.Back * 4);
	}

	private void YawMinusButtonOnPressed()
	{
		RotateCursor(Vector3.Up, -_rotationStep, false);
	}

	private void YawPlusButtonOnPressed()
	{
		RotateCursor(Vector3.Up, _rotationStep, false);
	}

	private void HSplitContainerOnDragged(long offset)
	{
		var width = BlockListContainer.Size.X;
		var itemWidth = 64;
		var sep = BlockListContainer.GetThemeConstant("separation");
		var columns = (int)Math.Floor((width + sep) / (itemWidth + sep));
		BlockListContainer.Columns = columns;
	}

	private void SetGridSizeSetting(int setting)
	{
		setting = int.Clamp(setting, -3, 3);
		_gridSizeSetting = setting;
		_gridScale = Mathf.Pow(2, setting);
		_gridHeightScale = float.Min(_gridScale, 2f);
		GridSizeLabel.Text = _gridScale.ToString(CultureInfo.InvariantCulture);
	}

	private void ConfirmNewDialogOnConfirmed()
	{
		CloseTrack();
		GameManager.Singleton.NewTrack();
		SetupOptions();
	}

	private async GDTaskVoid FileDialogOnFileSelected(string path)
	{
		if (FileDialog.FileMode == FileDialog.FileModeEnum.OpenFile)
		{
			CloseTrack();
			GameManager.Singleton.OpenTrack(path);
			SetupOptions();
			_grid = Transform3D.Identity;

			foreach (var block in Track.FindChildren("*", "Block").Cast<Block>()) ConnectBlockSignals(block);
		}
		else if (FileDialog.FileMode == FileDialog.FileModeEnum.SaveFile)
		{
			await TakeScreenshot();
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

		Track.Options.Uid = "0";
		GameModeController.CurrentGameMode.InitTrack(Track);
		GameManager.Singleton.Play();
		await GDTask.ToSignal(GameManager.Singleton, GameManager.SignalName.StoppedPlaying);

		IsRunning = true;
	}

	private IEnumerable<string> GetBlockPaths(string basePath, string dirPath = "")
	{
		foreach (var path in ResourceLoader.ListDirectory(basePath + dirPath).ToList().Order())
		{
			var subpath = dirPath.PathJoin(path);
			if (ResourceLoader.Exists(basePath.PathJoin(subpath), "BlockRecord"))
				yield return subpath;
		}
	}

	private IEnumerable<BlockRecord> GetBlockRecords(string dirPath)
	{
		return GetBlockPaths(BlockPath, dirPath)
			.Order()
			.Select(path => ResourceLoader.Load(BlockPath.PathJoin(path), "BlockRecord"))
			.Where(resource => resource is BlockRecord)
			.Cast<BlockRecord>();
	}

	public override void _Process(double delta)
	{
		if (ScreenshotUi.Visible || _takingScreenshot)
		{
			_cursor.Visible = false;
		}
		else
		{
			_cursor.Visible = true;
			ProcessEditor((float) delta);
		}
			
	}

	public void ProcessEditor(float delta)
	{
		UpdateCamera(delta);

		_cursor.GlobalPosition = GetWorldMousePosition();
		_cursor.Visible = true;

		if (_hoveredBlock != null && !IsInstanceValid(_hoveredBlock)) _hoveredBlock = null;

		if (_mode == Mode.Erase || _mode == Mode.Pick || _mode == Mode.GridPick)
		{
			_cursor.Visible = false;

			if (_hoveredBlock != null)
			{
				if (_mode == Mode.Erase)
					_hoveredBlock.SetMaterialOverlay(BlockEraseHighlightMaterial);
				else
					_hoveredBlock.SetMaterialOverlay(BlockHighlightMaterial);
			}
		}
		
		if (_mode == Mode.Normal)
			if (_hoveredBlock != null)
				_hoveredBlock.SetMaterialOverlay(null);

		DrawGrid();
	}

	private void DrawGrid()
	{
		GridMesh.ClearSurfaces();
		GridMesh.SurfaceBegin(Mesh.PrimitiveType.Lines);

		var center = GetGridMousePosition(8, 1);
		var scale = new Vector3(8, 1, 8);

		for (int i = -GridMeshSize; i < GridMeshSize; i++)
		{
			GridMesh.SurfaceAddVertex(_grid * (center + scale * new Vector3(i + 0.5f, 0.05f, -GridMeshSize + 0.5f)));
			GridMesh.SurfaceAddVertex(_grid * (center + scale * new Vector3(i + 0.5f, 0.05f, GridMeshSize - 0.5f)));
		}
		for (int i = -GridMeshSize; i < GridMeshSize; i++)
		{
			GridMesh.SurfaceAddVertex(_grid * (center + scale * new Vector3(-GridMeshSize + 0.5f, 0.05f, i + 0.5f)));
			GridMesh.SurfaceAddVertex(_grid * (center + scale * new Vector3(GridMeshSize - 0.5f, 0.05f, i + 0.5f)));
		}
		
		GridMesh.SurfaceEnd();
	}

	private void CreateCursor()
	{
		_cursor?.QueueFree();

		_cursor = CurrentBlockRecord.Instantiate();

		AddChild(_cursor);
		_cursor.Basis = _grid.Basis;
		_cursor.RotateObjectLocal(Vector3.Up, -Single.Pi * _rotation / 2);
		//_cursor.Transform = _cursor.Transform.Rounded();
		
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
		var existingBlock = GetExactlyMatchingBlock(_cursor);
		if (existingBlock != null)
			return;

		_cursor.SetMaterialOverlay(null);

		var orientation = _cursor.Basis;
		var transform = _cursor.GlobalTransform.Rounded();
		_cursor.GetParent().RemoveChild(_cursor);
		Track.AddChild(_cursor, forceReadableName: true);
		_cursor.GlobalTransform = transform;
		
		_cursor.Owner = Track;
		
		ConnectBlockSignals(_cursor);
		_hoveredBlock = _cursor;

		UiSoundPlayer.Singleton.BlockPlacedSound.Play();

		InvalidateTrack();
		
		_cursor = null;
		CreateCursor();
		_cursor.Basis = orientation;
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
		_cursor.RotateObjectLocal(Vector3.Up, -Single.Pi / 2);

		_rotation = (_rotation + 1) % 4;
	}

	private void UpdateCamera(float delta)
	{
		if (Input.IsActionPressed("editor_left"))
			Camera.GlobalPosition += delta * Camera.Radius * Camera.CameraStickBase.GlobalBasis.X;
		if (Input.IsActionPressed("editor_right"))
			Camera.GlobalPosition -= delta * Camera.Radius * Camera.CameraStickBase.GlobalBasis.X;
		if (Input.IsActionPressed("editor_forward"))
			Camera.GlobalPosition += delta * Camera.Radius * Camera.CameraStickBase.GlobalBasis.X.Cross(Vector3.Up);
		if (Input.IsActionPressed("editor_back"))
			Camera.GlobalPosition -= delta * Camera.Radius * Camera.CameraStickBase.GlobalBasis.X.Cross(Vector3.Up);
		if (Input.IsActionPressed("editor_up"))
			Camera.GlobalPosition += delta * Camera.Radius * Vector3.Up;
		if (Input.IsActionPressed("editor_down"))
			Camera.GlobalPosition += delta * Camera.Radius * Vector3.Down;
	}

	private Vector3 ProjectMousePosition()
	{
		var mousePosition = EditorViewport.GetMousePosition();
		if (!_mouseOverViewport)
			mousePosition = EditorViewport.Size / 2;
		
		var camera = EditorViewport.GetCamera3D();
		
		var toGrid = _grid.AffineInverse();
		var from = toGrid * camera.ProjectRayOrigin(mousePosition);
		var dir = toGrid.Basis * camera.ProjectRayNormal(mousePosition);
		
		var plane = new Plane(Vector3.Up, new Vector3(0, _gridHeightScale * GetYLevelRounded(_gridHeightScale), 0));
		var intersection = plane.IntersectsRay(from, dir) ?? Vector3.Zero;

		return new Vector3(intersection.X, _gridHeightScale * GetYLevelRounded(_gridHeightScale), intersection.Z);
	}

	private float GetYLevelRounded(float heightScale)
	{
		return float.Round(_yLevel / heightScale);
	}

	private Vector3 GetGridMousePosition(float scale, float heightScale)
	{
		var pos = ProjectMousePosition();
		return new Vector3(scale, heightScale, scale) * new Vector3(float.Round(pos.X / scale), GetYLevelRounded(heightScale), float.Round(pos.Z / scale));
	}

	private Vector3 GetWorldMousePosition()
	{
		return _grid * GetGridMousePosition(_gridScale, _gridHeightScale);
	}

	public void ViewportInput(InputEvent @event)
	{
		if (!IsRunning)
			return;
		
		EditorViewportContainer.GrabFocus();

		if (@event is InputEventKey keyEvent)
		{
			if (keyEvent.PhysicalKeycode == Key.X)
				EraseButton.SetPressed(keyEvent.Pressed);
			else if (keyEvent.PhysicalKeycode == Key.Ctrl)
				PickButton.SetPressed(keyEvent.Pressed);
			else if (keyEvent.PhysicalKeycode == Key.G)
				GridPickButton.SetPressed(keyEvent.Pressed);
		}

		if (_mode == Mode.Normal)
		{
			if (@event.IsActionPressed("editor_left_click", exactMatch: true))
				PlaceCursorBlock();
			if (@event.IsActionPressed("editor_right_click", exactMatch: true))
				RotateCursor();
			if (@event.IsActionPressed("editor_go_down", exactMatch: true))
			{
				_yLevel -= _gridHeightScale;
				Camera.GlobalPosition += _gridHeightScale * (_grid.Basis * Vector3.Down);
			}
			if (@event.IsActionPressed("editor_go_up", exactMatch: true))
			{
				_yLevel += _gridHeightScale;
				Camera.GlobalPosition += _gridHeightScale * (_grid.Basis * Vector3.Up);
			}
			if (@event.IsActionPressed("editor_zoom_in", exactMatch: true))
			{
				Camera.Radius = Math.Max(8, Camera.Radius - 3);
			}
			if (@event.IsActionPressed("editor_zoom_out", exactMatch: true))
			{
				Camera.Radius += 3;
			}
			
			if (@event.IsActionPressed("editor_yawplus", allowEcho: true))
				YawPlusButtonOnPressed();
			if (@event.IsActionPressed("editor_yawminus", allowEcho: true))
				YawMinusButtonOnPressed();
			if (@event.IsActionPressed("editor_pitchplus", allowEcho: true))
				PitchPlusButtonOnPressed();
			if (@event.IsActionPressed("editor_pitchminus", allowEcho: true))
				PitchMinusButtonOnPressed();
			if (@event.IsActionPressed("editor_rollplus", allowEcho: true))
				RollPlusButtonOnPressed();
			if (@event.IsActionPressed("editor_rollminus", allowEcho: true))
				RollMinusButtonOnPressed();
			if (@event.IsActionPressed("editor_reset_rotation"))
				_cursor.Basis = Basis.Identity;

			if (@event is InputEventMouseMotion mouseMotionEvent
				&& mouseMotionEvent.GetModifiersMask() == KeyModifierMask.MaskAlt
				&& mouseMotionEvent.ButtonMask == MouseButtonMask.Left)
			{
				Camera.RotateCamera(mouseMotionEvent.ScreenRelative / EditorViewport.Size.Y);
			}
		}

		if (_mode == Mode.Erase)
		{
			if (@event is InputEventMouseButton mouseEvent)
				if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.IsPressed())
					EraseHoveredBlock();
		}
		
		if (_mode == Mode.Pick)
		{
			if (@event is InputEventMouseButton mouseEvent)
				if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.IsPressed())
					PickHoveredBlock();
		}
		
		if (_mode == Mode.GridPick)
		{
			if (@event is InputEventMouseButton mouseEvent)
				if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.IsPressed())
					GridPickHoveredBlock();
		}
	}

	private void RotateCursor(Vector3 axis, float angle, bool local = true, Vector3 origin = default)
	{
		if (local)
		{
			// оставь надежду, всяк сюда входящий
			var oldOrigin = _cursor.GlobalPosition;
			
			var forward = Camera.CameraStickBase.GlobalBasis.X.Cross(Vector3.Up);
			var directions = new List<Vector3> { _cursor.GlobalBasis.X, -_cursor.GlobalBasis.X, _cursor.GlobalBasis.Z, -_cursor.GlobalBasis.Z };
			var basisZ = directions.MinBy(vector => vector.DistanceTo(forward));
			var basisX = _cursor.GlobalBasis.Y.Cross(basisZ);
			
			var forwardTransform = new Transform3D(
					basisX,
					_cursor.GlobalBasis.Y,
					basisZ,
					Vector3.Zero)
				.Orthonormalized();
			
			var shift = _rotationShiftOrigin ? -origin : Vector3.Zero;
			var shiftTransform = new Transform3D(Basis.Identity, shift);

			var transform = new Transform3D(_cursor.GlobalBasis.Inverse(), Vector3.Zero) * forwardTransform *
							shiftTransform;

			_cursor.GlobalTransform = _cursor.GlobalTransform * transform *
								(new Transform3D(new Basis(axis, angle), Vector3.Zero)) *
								transform.Inverse();
			
			if (_rotationShiftOrigin)
			{
				_grid = _cursor.GlobalTransform;
				_yLevel = 0;
			}
		}
		else
		{
			_cursor.Rotate(_grid.Basis * axis, angle);
		}
		
		_cursor.GlobalTransform = _cursor.GlobalTransform.Orthonormalized();
	}

	private void EraseBlock(Block block)
	{
		Track.RemoveChild(block);
		block.QueueFree();
		InvalidateTrack();
		
		if (_hoveredBlock == block)
			_hoveredBlock = null;
	}

	private void EraseHoveredBlock()
	{
		if (_hoveredBlock != null)
		{
			EraseBlock(_hoveredBlock);

			UiSoundPlayer.Singleton.BlockErasedSound.Play();
		}
	}
	
	private void PickHoveredBlock()
	{
		if (_hoveredBlock != null)
		{
			CurrentBlockRecord = _hoveredBlock.Record;
			CreateCursor();
			_cursor.GlobalBasis = _hoveredBlock.GlobalBasis;
			
			UiSoundPlayer.Singleton.BlockPlacedSound.Play();

			PickButton.SetPressed(false);
		}
	}
	
	private void GridPickHoveredBlock()
	{
		if (_hoveredBlock != null)
		{
			_grid = _hoveredBlock.GlobalTransform;
			_yLevel = 0;
			_cursor.GlobalBasis = _hoveredBlock.GlobalBasis;

			GridPickButton.SetPressed(false);
		}
	}

	private Block GetExactlyMatchingBlock(Block block)
	{
		foreach (var other in Track.GetChildren().Where(node => node is Block).Cast<Block>())
			if (other.Record == block.Record && other.Transform.IsEqualApprox(block.Transform))
				return block;

		return null;
	}

	private void OnBlockButtonPressed(BlockRecord blockRecord)
	{
		CurrentBlockRecord = blockRecord;

		CreateCursor();
	}

	private void ConfirmQuitDialogOnConfirmed()
	{
		IsRunning = false;
		EmitSignalExited();
	}

	private async GDTaskVoid SetDirectory(string path)
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
			button.Pressed += () => SetDirectory(baseDir).Forget();

			DirectoryListContainer.AddChild(button);
		}

		foreach (var subDir in dir.GetDirectories())
		{
			if (subDir.GetFile().StartsWith('_'))
				continue;

			var subDirPath = _blockDirectory.PathJoin(subDir);

			var button = new Button();
			button.Text = subDir;
			button.Pressed += () => SetDirectory(subDirPath).Forget();

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

	public void SetupOptions()
	{
		OptionsTree.Clear();
		var root = OptionsTree.CreateItem();

		var trackName = OptionsTree.CreateItem(root);
		trackName.SetText(0, "TrackName");
		trackName.SetText(1, Track.Options.Name);
		trackName.SetEditable(1, true);

		var authorName = OptionsTree.CreateItem(root);
		authorName.SetText(0, "AuthorName");
		authorName.SetText(1, Track.Options.AuthorName);
		authorName.SetEditable(1, true);

		var mapType = OptionsTree.CreateItem(root);
		mapType.SetText(0, "TrackType");
		mapType.SetCellMode(1, TreeItem.TreeCellMode.Range);
		mapType.SetText(1, Track.Options.TrackType);
		mapType.SetEditable(1, true);

		var carType = OptionsTree.CreateItem(root);
		carType.SetText(0, "CarType");
		carType.SetCellMode(1, TreeItem.TreeCellMode.Range);

		var paths = GameManager.Singleton.LoadCarList();
		foreach (var carPath in paths) carType.SetText(1, carType.GetText(1) + carPath + ",");
		carType.SetText(1, carType.GetText(1).Trim(','));
		carType.SetEditable(1, true);

		var lapsCount = OptionsTree.CreateItem(root);
		lapsCount.SetText(0, "Laps");
		lapsCount.SetCellMode(1, TreeItem.TreeCellMode.Range);
		lapsCount.SetRange(1, Track.Options.Laps);
		lapsCount.SetEditable(1, true);
		
		var dayTime = OptionsTree.CreateItem(root);
		dayTime.SetText(0, "DayTime");
		dayTime.SetCellMode(1, TreeItem.TreeCellMode.Range);
		dayTime.SetRange(1, Track.Options.StartDayTime);
		dayTime.SetRangeConfig(1, 1, 24, 1, false);
		dayTime.SetEditable(1, true);

		LoadScreenshot();
	}

	public void OptionEdited()
	{
		var editedItem = OptionsTree.GetEdited();
		var editedColumn = OptionsTree.GetEditedColumn();

		switch (editedItem.GetText(0))
		{
			case "TrackName":
				Track.Options.Name = editedItem.GetText(editedColumn);
				break;
			case "AuthorName":
				Track.Options.AuthorName = editedItem.GetText(editedColumn);
				break;
			case "TrackType":
				Track.Options.TrackType = editedItem.GetText(editedColumn);
				break;
			case "CarType":
				Track.Options.CarType =
					editedItem.GetText(editedColumn).Split(",")[(int)editedItem.GetRange(editedColumn)];
				break;
			case "Laps":
				Track.Options.Laps =
					(int) editedItem.GetRange(editedColumn);
				break;
			case "DayTime":
				Track.Options.StartDayTime = (int)editedItem.GetRange(editedColumn);
				Track.GetNode("Sky3D").GetNode("TimeOfDay").Set("current_time", (float)Track.Options.StartDayTime);
				break;
		}
	}

	private void CloseTrack()
	{
	}

	private void InvalidateTrack()
	{
		Track.Options.AuthorTime = 0;
	}
	
	private void SetThumbnailButtonOnPressed()
	{
		ScreenshotUi.Visible = true;
		ScreenshotCamera.Active = true;
		
		ScreenshotCamera.Load(Track.Options.PreviewCameraPosition);
	}
	
	private void ThumbnailCancelButtonOnPressed()
	{
		ScreenshotUi.Visible = false;
		ScreenshotCamera.Active = false;
	}

	private async GDTaskVoid ThumbnailConfirmButtonOnPressed()
	{
		Track.Options.PreviewCameraPosition = ScreenshotCamera.Save();
		
		await TakeScreenshot();
		
		ScreenshotUi.Visible = false;
		ScreenshotCamera.Active = false;
	}

	private async GDTask TakeScreenshot()
	{
		_cursor.Visible = false;
		_takingScreenshot = true;
		ScreenshotCamera.Load(Track.Options.PreviewCameraPosition);
		
		ScreenshotViewportContainer.Stretch = false;
		ScreenshotViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
		ScreenshotViewport.Size = new Vector2I(512, 512);
		
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		Image img = ScreenshotViewport.GetTexture().GetImage();

		ScreenshotViewportContainer.Stretch = true;

		_cursor.Visible = true;
		_takingScreenshot = false;
		
		img.Resize(512, 512, Image.Interpolation.Bilinear);
		var buffer = img.SaveJpgToBuffer();
		Track.Options.PreviewImage = Marshalls.RawToBase64(buffer);
		
		LoadScreenshot();
	}

	private void LoadScreenshot()
	{
		var image = new Image();
		if (Track.Options.PreviewImage == "")
		{
			ThumbnailTextureRect.Texture = null;
			return;
		}
		
		if (image.LoadJpgFromBuffer(Marshalls.Base64ToRaw(Track.Options.PreviewImage)) == Error.Ok)
		{
			ThumbnailTextureRect.Texture = ImageTexture.CreateFromImage(image);
		}
	}
}
