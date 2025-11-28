using System.Collections.Generic;
using Godot;
using racingGame;

public partial class CarWheel : ShapeCast3D
{
	[Export] public WheelConfig Config;
	
	[ExportCategory("Setup")]
	[Export]
	public bool IsFrontWheel;
	[Export]
	public bool IsRearWheel;
	[Export] 
	public Node3D WheelModel;
	[Export]
	public float SkidmarkWidth;
	
	[ExportCategory("Builtin")]
	[Export]
	private MeshInstance3D SkidmarkMeshInstance;
	[Export]
	private int SkidmarkCapacity = 512;
	[Export]
	private Curve SkidmarkOpacityCurve;
	
	private ImmediateMesh _skidmarkMesh;
	private RingBuffer<SkidmarkSegment> _skidmarkLines;
	private Vector3 _previousSkidmarkPosition;
	private Vector3 _previousSkidmarkLeft;
	private bool _isSliding = false;
	
	public override void _Ready()
	{
		_skidmarkLines = new RingBuffer<SkidmarkSegment>(SkidmarkCapacity);
		
		_previousSkidmarkPosition = GlobalPosition;
		_skidmarkMesh = new ImmediateMesh();
		SkidmarkMeshInstance.Mesh = _skidmarkMesh;
	}

	public void Slide(Vector3 position, Vector3 velocity)
	{
		var left = velocity.Cross(Basis.Y).Normalized();
		
		if (_isSliding)
		{
			_skidmarkLines.Add(new SkidmarkSegment
			{
				StartLeft = _previousSkidmarkPosition + _previousSkidmarkLeft * SkidmarkWidth / 2,
				StartRight = _previousSkidmarkPosition - _previousSkidmarkLeft * SkidmarkWidth / 2,
				EndLeft = position + left * SkidmarkWidth / 2,
				EndRight = position - left * SkidmarkWidth / 2,
			});
			
			CreateSkidmarkMesh();
		}

		_isSliding = true;
		_previousSkidmarkPosition = position;
		_previousSkidmarkLeft = left;
	}

	public void StopSliding()
	{
		_isSliding = false;
	}

	private void CreateSkidmarkMesh()
	{
		if (_skidmarkLines.Count > 0)
		{
			_skidmarkMesh.ClearSurfaces();
			_skidmarkMesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);

			for (int i = 0; i < _skidmarkLines.Count; i++)
			{
				var line = _skidmarkLines[i];
				var t1 = ((float) i + SkidmarkCapacity - _skidmarkLines.Count) / SkidmarkCapacity;
				var t2 = ((float) i + 1 + SkidmarkCapacity - _skidmarkLines.Count) / SkidmarkCapacity;
				var opacity1 = SkidmarkOpacityCurve.SampleBaked(t1);
				var opacity2 = SkidmarkOpacityCurve.SampleBaked(t2);
				
				_skidmarkMesh.SurfaceAddVertex(line.StartLeft);
				_skidmarkMesh.SurfaceAddVertex(line.StartRight);
				_skidmarkMesh.SurfaceAddVertex(line.EndLeft);
				_skidmarkMesh.SurfaceSetColor(new Color(Colors.White, opacity1));
				
				_skidmarkMesh.SurfaceAddVertex(line.EndRight);
				_skidmarkMesh.SurfaceAddVertex(line.EndLeft);
				_skidmarkMesh.SurfaceAddVertex(line.StartRight);
				_skidmarkMesh.SurfaceSetColor(new Color(Colors.White, opacity2));
			}
		
			_skidmarkMesh.SurfaceEnd();
		}
	}
}
