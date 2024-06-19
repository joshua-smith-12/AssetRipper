using AsmResolver.DotNet;
using AssetRipper.Assets;
using AssetRipper.Assets.Collections;
using AssetRipper.Assets.Generics;
using AssetRipper.Assets.Metadata;
using AssetRipper.Decompilation.CSharp;
using AssetRipper.Export.Modules.Shaders.IO;
using AssetRipper.Export.UnityProjects.Scripts;
using AssetRipper.Export.UnityProjects.Shaders;
using AssetRipper.GUI.Web.Paths;
using AssetRipper.Import.Structure.Assembly;
using AssetRipper.Import.Structure.Assembly.Managers;
using AssetRipper.Import.Structure.Assembly.Serializable;
using AssetRipper.SourceGenerated.Classes.ClassID_1;
using AssetRipper.SourceGenerated.Classes.ClassID_114;
using AssetRipper.SourceGenerated.Classes.ClassID_115;
using AssetRipper.SourceGenerated.Classes.ClassID_21;
using AssetRipper.SourceGenerated.Classes.ClassID_4;
using AssetRipper.SourceGenerated.Classes.ClassID_48;
using AssetRipper.SourceGenerated.Classes.ClassID_49;
using AssetRipper.SourceGenerated.Extensions;
using AssetRipper.Text.Html;
using System.IO;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AssetRipper.GUI.Web.Pages.Assets;

internal sealed class TreeViewTab : HtmlTab
{
	public IUnityObjectBase Asset;

	public override string DisplayName => "Asset Tree";

	public override string HtmlName => "treeview";

	public TreeViewTab(IUnityObjectBase asset)
	{
		Asset = asset;
	}

	public override void Write(TextWriter writer)
	{
		using (new Table(writer).WithClass("table").End())
		{
			using (new Tbody(writer).End())
			{
				var pptr = Asset.FetchDependencies().First().Item2;
				DoDependencies(writer, Asset.Collection.TryGetAsset(pptr) as IGameObject, 0);
			}
		}
	}

	public void DoDependencies(TextWriter writer, IGameObject? asset, int depth)
	{
		if (asset == null) return;
		var transform = asset.GetComponent<ITransform>();
		if (transform == null) return;
		
		foreach (var childData in transform.Children_C4)
		{
			var child = asset.Collection.TryGetAsset(childData.PathID) as ITransform;
			var goData = child.GameObject_C4;
			var dependency = asset.Collection.TryGetAsset(goData.PathID) as IGameObject;
			if (dependency == null || dependency.ClassName != "GameObject") continue;

			using (new Tr(writer).End())
			{
				using (new Td(writer).End())
				{
					var tabs = "";
					for (var idx = 0; idx < depth; idx++) tabs += "\t";
					PathLinking.WriteLink(writer, dependency, name: tabs + dependency.GetBestName(), @class: "btn btn-dark p-0 m-0 text-pre");
					DoDependencies(writer, dependency, depth + 1);
				}
			}
		}
	}
}
