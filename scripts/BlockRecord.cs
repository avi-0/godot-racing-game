using Godot;
using System;
using System.IO;
using System.Linq;
using racingGame.blocks;

[GlobalClass]
[Tool]
public partial class BlockRecord : Resource
{
    [Export] public PackedScene SourceScene;

    [Export] public PackedScene Scene;

    [ExportToolButton("Generate from source")]
    public Callable GenerateButton => Callable.From(GenerateScene);
    
    public void GenerateScene()
    {
        // creating node (somewhere in current scene, doesn't matter)
        
        var root = EditorInterface.Singleton.GetEditedSceneRoot();
        var node = new Block();
        GD.Print(node.GetScript());
        root.AddChild(node);

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

        // pack and save
        
        var packedScene = new PackedScene();
        packedScene.Pack(node);
        packedScene.TakeOverPath(ResourcePath.GetBaseName() + "_scene.tscn");

        ResourceSaver.Singleton.Save(packedScene);
        
        
        Scene = packedScene;
        ResourceSaver.Singleton.Save(this);
        
        // clean up
        
        root.RemoveChild(node);
        node.QueueFree();
    }
}
