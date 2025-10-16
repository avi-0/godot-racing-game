using Godot;

namespace racingGame;

public partial class Editor : Node
{
	public const float GridSize = 8;
	
	[Export] public float CameraSpeed;
	
	[Export] public Camera3D Camera;

	[Export] public PackedScene BlockScene;

	[Export] public Node3D TrackBlocksNode;

	private blocks.Block Cursor;

	public override void _Ready()
	{
		CreateCursor();
	}

	public override void _Process(double delta)
	{
		UpdateCamera((float) delta);

		Cursor.GlobalPosition = GetGridMousePosition();
	}

	private void CreateCursor()
	{
		Cursor = BlockScene.Instantiate<blocks.Block>();
		Cursor.PhysicsInterpolationMode = PhysicsInterpolationModeEnum.Off;
		AddChild(Cursor);
	}

	private void PlaceCursorBlock()
	{
		Cursor.Reparent(TrackBlocksNode, true);
		CreateCursor();

		UiSoundPlayer.__Instance.PlayBlockPlaced();
	}

	private void RotateCursor()
	{
		Cursor.RotateY(float.DegreesToRadians(90));
		Cursor.GlobalRotationDegrees = Cursor.GlobalRotationDegrees.Round();
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
		var plane = new Plane(Vector3.Up, Vector3.Zero);
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
			GridSize * (float.Round(pos.X / GridSize - 0.5f) + 0.5f),
			0,
			GridSize * (float.Round(pos.Y / GridSize - 0.5f) + 0.5f));
	}

	public override void _UnhandledInput(InputEvent @event)
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
		}
	}
}
