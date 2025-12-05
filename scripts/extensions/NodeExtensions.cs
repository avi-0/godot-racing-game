using Godot;

namespace racingGame.extensions;

public static class NodeExtensions
{
	public static void DestroyAllChildren(this Node node)
	{
		foreach (var child in node.GetChildren())
		{
			node.RemoveChild(child);
			child.QueueFree();
		}
	}
}
