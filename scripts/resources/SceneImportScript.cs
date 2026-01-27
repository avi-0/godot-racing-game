#if TOOLS
using Godot;

namespace racingGame;
[Tool]
public partial class SceneImportScript : EditorScenePostImport
{
	public const string MaterialRootPath = "res://assets/cartopia/materials/";
	
	public override Node _PostImport(Node scene)
	{
		ResourceLoader.Singleton.ListDirectory(MaterialRootPath);
		
		LinkMaterials(scene);
		
		return scene;
	}

	private void LinkMaterials(Node node)
	{
		if (node is MeshInstance3D meshInstance)
		{
			for (int i = 0; i < meshInstance.Mesh.GetSurfaceCount(); i++)
			{
				var material = meshInstance.Mesh.SurfaceGetMaterial(i);
				GD.Print(material.ResourceName);
				
				var replacementMaterial = ResourceLoader.Load<Material>(MaterialRootPath.PathJoin(material.ResourceName + ".tres"));
				if (replacementMaterial != null)
				{
					meshInstance.Mesh.SurfaceSetMaterial(i, replacementMaterial);
				}
				else
				{
					GD.PushError("Couldn't find material: " + material.ResourceName);
				}
			}
		}

		foreach (var child in node.GetChildren())
		{
			LinkMaterials(child);
		}
	}
}
#endif