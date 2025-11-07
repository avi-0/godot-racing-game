using Godot;
using Newtonsoft.Json;

namespace racingGame;

public class BlockPlacementData
{
	[JsonConverter(typeof(Transform3DConverter))]
	public Transform3D Transform;

	public string BlockRecordPath;
}