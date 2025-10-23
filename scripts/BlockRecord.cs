using Godot;
using System;
using System.IO;
using System.Linq;
using Fractural.Tasks;
using racingGame.blocks;

[GlobalClass]
[Tool]
public partial class BlockRecord : Resource
{
    [Export] public PackedScene SourceScene;

    [Export] public PackedScene Scene;

    [Export] public Texture2D ThumbnailTexture;

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

        foreach (var meshChild in model.FindChildren("*", "MeshInstance3D", true, false).Cast<MeshInstance3D>())
        {
            meshChild.CreateTrimeshCollision();
        }
        
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
        ThumbnailTexture.TakeOverPath(ResourcePath.GetBaseName() + "_img.png");
        ResourceSaver.Singleton.Save(ThumbnailTexture);

        // pack and save
        
        var packedScene = new PackedScene();
        packedScene.Pack(node);
        packedScene.TakeOverPath(ResourcePath.GetBaseName() + "_scene.tscn");

        ResourceSaver.Singleton.Save(packedScene);
        
        Scene = packedScene;
        ResourceSaver.Singleton.Save(this);
        
        // clean up
        
        EditorInterface.Singleton.CloseScene();
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
}
