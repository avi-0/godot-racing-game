using System.Diagnostics;
using System.Linq;
using Fractural.Tasks;
using Godot;

namespace racingGame;

[GlobalClass]
[Tool]
public partial class BlockRecord : Resource
{
	[Export] public PackedScene Scene;
	[Export] public PackedScene SourceScene;

	[Export] public Texture2D ThumbnailTexture;

#if TOOLS
	[ExportToolButton("Generate from source")]
	public Callable GenerateButton => Callable.From(() => GenerateScene().Forget());

	public async GDTask GenerateScene()
	{
		// creating node (somewhere in current scene, doesn't matter)

		EditorInterface.Singleton.OpenSceneFromPath("res://scenes/model_import_scene.tscn");
		var root = EditorInterface.Singleton.GetEditedSceneRoot() as SubViewport;
		Debug.Assert(root != null, nameof(root) + " != null");
		var modelBase = root.GetNode("ModelBase");
		var camera = root.GetNode<Camera3D>("Camera3D");

		var node = new Block();
		node.Name = SourceScene.ResourcePath.GetFile().GetBaseName();
		modelBase.AddChild(node);

		// this adds the node to the EDITED scene, not actually needed but lets us see the node in the editor
		// and helps make sure we delete it later
		node.Owner = root;
		// other nodes will have .Owner = node

		// editing

		var model = SourceScene.Instantiate<Node3D>();
		node.AddChild(model);
		model.Owner = node;
		model.SceneFilePath = ""; // break off the instance relationship so we can edit nodes freely

		model.SetScale(8 * Vector3.One);

		// process stuff (add collisions etc.)

		ProcessModel(node, model);

		// set owner for everything

		foreach (var child in node.FindChildren("*", "", true, false)) child.Owner = node;

		// take screenshot

		var bounds = CalculateCameraSize(model);
		camera.Size = bounds.GetLongestAxisSize();
		camera.GlobalTranslate(new Vector3(bounds.GetCenter().X, bounds.GetCenter().Y, 0));

		root.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);

		var image = root.GetTexture().GetImage();
		image.GenerateMipmaps();
		ThumbnailTexture = ImageTexture.CreateFromImage(image);
		ThumbnailTexture.TakeOverPath(ResourcePath.GetBaseDir()
			.PathJoin("/_images/" + ResourcePath.GetFile().GetBaseName() + ".png"));
		DirAccess.MakeDirRecursiveAbsolute(ThumbnailTexture.ResourcePath.GetBaseDir());
		ResourceSaver.Singleton.Save(ThumbnailTexture);

		// pack and save

		var packedScene = new PackedScene();
		packedScene.Pack(node);
		packedScene.TakeOverPath(ResourcePath.GetBaseDir()
			.PathJoin("/_scenes/" + ResourcePath.GetFile().GetBaseName() + ".tscn"));
		DirAccess.MakeDirRecursiveAbsolute(packedScene.ResourcePath.GetBaseDir());
		ResourceSaver.Singleton.Save(packedScene);

		Scene = packedScene;
		DirAccess.MakeDirRecursiveAbsolute(ResourcePath.GetBaseDir());
		ResourceSaver.Singleton.Save(this);

		// clean up

		EditorInterface.Singleton.CloseScene();
	}

	private static bool NameHasTag(Node node, string tag)
	{
		return node.Name.ToString().ToLower().Contains(tag);
	}

	private static void ProcessModel(Block block, Node3D model)
	{
		foreach (var meshChild in model.FindChildren("*", "MeshInstance3D", true, false).Cast<MeshInstance3D>())
		{
			if (NameHasTag(meshChild, "finishhitbox"))
			{
				ConvertMeshToHitbox(block, meshChild, "finish_hitbox");
				block.IsFinish = true;

				continue;
			}

			if (NameHasTag(meshChild, "checkpointhitbox"))
			{
				ConvertMeshToHitbox(block, meshChild, "checkpoint_hitbox");
				block.IsCheckpoint = true;

				continue;
			}

			if (NameHasTag(meshChild, "spawnpoint"))
			{
				ConvertMeshToSpawnPoint(block, meshChild);

				if (NameHasTag(meshChild, "startspawnpoint")) block.IsStart = true;

				continue;
			}

			meshChild.CreateTrimeshCollision();
		}
	}

	private static void ConvertMeshToHitbox(Block block, MeshInstance3D meshChild, string group)
	{
		var shape = meshChild.Mesh.CreateConvexShape();
		var area = new Area3D();
		var collisionShape = new CollisionShape3D();

		block.AddChild(area);

		area.Name = meshChild.Name;
		area.AddChild(collisionShape);
		area.AddToGroup(group, true);
		area.CollisionLayer = GameManager.BlockLayer;
		area.CollisionMask = GameManager.CarLayer;

		collisionShape.Name = meshChild.Name;
		collisionShape.Shape = shape;
		collisionShape.GlobalTransform = meshChild.GlobalTransform;

		meshChild.GetParent().RemoveChild(meshChild);
		meshChild.QueueFree();
	}

	private static void ConvertMeshToSpawnPoint(Block block, MeshInstance3D meshChild)
	{
		var node = new Node3D();

		block.AddChild(node);
		block.SpawnPointNode = node;

		node.Name = meshChild.Name;
		node.GlobalTransform = meshChild.GlobalTransform;

		meshChild.GetParent().RemoveChild(meshChild);
		meshChild.QueueFree();
	}

	private Aabb CalculateCameraSize(Node3D node)
	{
		var combinedBounds = new Aabb();
		var first = true;
		foreach (var meshChild in node.FindChildren("*", "MeshInstance3D", true, false).Cast<MeshInstance3D>())
		{
			var meshAabb = meshChild.GetAabb();
			var globalAabb = meshChild.GlobalTransform * meshAabb;

			if (first)
			{
				combinedBounds = globalAabb;
				first = false;
			}
			else
			{
				combinedBounds = combinedBounds.Merge(globalAabb);
			}
		}

		return combinedBounds;
	}
#endif
}