using Godot;
using System;
using System.IO;
using System.Linq;
using Fractural.Tasks;

namespace racingGame;

[GlobalClass]
[Tool]
public partial class BlockRecord : Resource
{
    [Export] public PackedScene SourceScene;

    [Export] public PackedScene Scene;

    [Export] public Texture2D ThumbnailTexture;

#if TOOLS
    [ExportToolButton("Generate from source")]
    public Callable GenerateButton => Callable.From(() => GenerateScene().Forget());
    
    public async GDTaskVoid GenerateScene()
    {
        // creating node (somewhere in current scene, doesn't matter)
        
        EditorInterface.Singleton.OpenSceneFromPath("res://scenes/model_import_scene.tscn");
        var root = EditorInterface.Singleton.GetEditedSceneRoot() as SubViewport;
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
        
        foreach (var child in node.FindChildren("*", "", true, false))
        {
            child.Owner = node;
        }
        
        // take screenshot

        var bounds = CalculateCameraSize(model);
        camera.Size = bounds.GetLongestAxisSize();
        camera.GlobalTranslate(new Vector3(bounds.GetCenter().X, bounds.GetCenter().Y, 0));
        
        root.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);

        var image = root.GetTexture().GetImage();
        image.GenerateMipmaps();
        ThumbnailTexture = ImageTexture.CreateFromImage(image);
        ThumbnailTexture.TakeOverPath(ResourcePath.GetBaseDir().PathJoin("/images/" + ResourcePath.GetFile().GetBaseName() + ".png"));
        ResourceSaver.Singleton.Save(ThumbnailTexture);

        // pack and save
        
        var packedScene = new PackedScene();
        packedScene.Pack(node);
        packedScene.TakeOverPath(ResourcePath.GetBaseDir().PathJoin("/scenes/" + ResourcePath.GetFile().GetBaseName() + ".tscn"));

        ResourceSaver.Singleton.Save(packedScene);
        
        Scene = packedScene;
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
                var shape = meshChild.Mesh.CreateConvexShape();
                var area = new Area3D();
                var collisionShape = new CollisionShape3D();
                
                block.AddChild(area);
                block.IsFinish = true;
                
                area.Name = meshChild.Name;
                area.AddChild(collisionShape);
                area.AddToGroup("finish_hitbox", true);
                area.CollisionLayer = GameManager.BlockLayer;
                area.CollisionMask = GameManager.CarLayer;
                
                collisionShape.Name = meshChild.Name;
                collisionShape.Shape = shape;
                collisionShape.GlobalTransform = meshChild.GlobalTransform;
                
                meshChild.GetParent().RemoveChild(meshChild);
                meshChild.QueueFree();
                
                continue;
            }
            
            meshChild.CreateTrimeshCollision();
        }
    }

    private Aabb CalculateCameraSize(Node3D node)
    {
        Aabb combinedBounds = new Aabb();
        bool first = true;
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
