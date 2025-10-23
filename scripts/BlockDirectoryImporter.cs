using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fractural.Tasks;

[GlobalClass]
[Tool]
public partial class BlockDirectoryImporter : Resource
{
    [Export(PropertyHint.Dir)] public string SourcePath;

    [ExportToolButton("Generate all")]
    public Callable GenerateAllButton => Callable.From(() => GenerateAll().Forget());

    public async GDTaskVoid GenerateAll()
    {
        foreach (var path in GetModelPaths(SourcePath + "/"))
        {
            var modelPath = SourcePath.PathJoin(path);
            var model = ResourceLoader.Load<PackedScene>(modelPath);
            var recordPath = ResourcePath.GetBaseDir().PathJoin(path.GetBaseName() + ".tres");

            var record = new BlockRecord();
            record.SourceScene = model;
            record.TakeOverPath(recordPath);

            ResourceSaver.Singleton.Save(record);
            
            record.GenerateScene();
            
            // mildly horrifying trick to slow down the process a bit
            // doing everything in one go crashes the editor for some reason xdd
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        }
        
        EditorInterface.Singleton.GetResourceFilesystem().ScanSources();
    }
    
    private IEnumerable<String> GetModelPaths(string basePath, string dirPath = "")
    {
        foreach (var path in ResourceLoader.ListDirectory(basePath + dirPath).ToList().Order())
        {
            var subpath = dirPath + path;
            if (ResourceLoader.Exists(basePath + subpath, "PackedScene") && subpath.EndsWith(".glb"))
                yield return subpath;

            foreach (var result in GetModelPaths(basePath, subpath))
                yield return result;
        }
    }
}
