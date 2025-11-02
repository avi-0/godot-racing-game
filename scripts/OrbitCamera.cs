using Godot;

public partial class OrbitCamera : Node3D
{
	[Export] public Camera3D Camera;
	[Export] public Node3D CameraStick;
	[Export] public Node3D CameraStickBase;

	public void UpdateYaw(float delta, Vector3 velocity)
	{
		var target = GetTargetYaw(velocity);
		CameraStickBase.Rotation = new Vector3(
			CameraStickBase.Rotation.X,
			Mathf.LerpAngle(target, CameraStickBase.Rotation.Y, Mathf.Exp(-5.0f * delta)),
			CameraStickBase.Rotation.Z
		);
	}

	private float GetTargetYaw(Vector3 dir)
	{
		return -dir.Slide(Vector3.Up).SignedAngleTo(Vector3.Back, Vector3.Up);
	}

	public void SnapYaw()
	{
		CameraStickBase.Rotation =
			new Vector3(CameraStickBase.Rotation.X, GetTargetYaw(GlobalBasis.Z), CameraStickBase.Rotation.Z);
	}
}