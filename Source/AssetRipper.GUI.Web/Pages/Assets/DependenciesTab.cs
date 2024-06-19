using AssetRipper.Assets;
using AssetRipper.Assets.Metadata;
using AssetRipper.GUI.Web.Paths;
using AssetRipper.SourceGenerated.Classes.ClassID_21;
using AssetRipper.SourceGenerated.Subclasses.UnityPropertySheet;
using AssetRipper.SourceGenerated.Subclasses.UnityTexEnv;

namespace AssetRipper.GUI.Web.Pages.Assets;

internal sealed class DependenciesTab(IUnityObjectBase asset) : HtmlTab
{
	public override string DisplayName => Localization.AssetTabDependencies;
	public override string HtmlName => "dependencies";
	public override bool Enabled => asset.FetchDependencies().Any(pair => !pair.Item2.IsNull);

	public string GetMaterialFullPath(PPtr pptr, IMaterial material, string original)
	{
		IUnityPropertySheet savedProperties = material.SavedProperties_C21;
		foreach(var property in savedProperties.TexEnvs_AssetDictionary_Utf8String_UnityTexEnv_5 ?? new())
		{
			if (property.Value.Texture.FileID == pptr.FileID && property.Value.Texture.PathID == pptr.PathID)
			{
				return property.Key;
			}
		}
		return original;
	}

	public override void Write(TextWriter writer)
	{
		var material = asset as IMaterial;
		using (new Table(writer).WithClass("table").End())
		{
			using (new Tbody(writer).End())
			{
				foreach ((string path, PPtr pptr) in asset.FetchDependencies())
				{
					string realPath = path;
					if (pptr.IsNull)
					{
						continue;
					}

					if (material != null)
					{
						realPath = GetMaterialFullPath(pptr, material, path);
					}

					using (new Tr(writer).End())
					{
						new Th(writer).Close(realPath);
						using (new Td(writer).End())
						{
							IUnityObjectBase? dependency = asset.Collection.TryGetAsset(pptr);
							if (dependency is null)
							{
								writer.WriteHtml("Missing");
							}
							else
							{
								PathLinking.WriteLink(writer, dependency);
							}
						}
					}
				}
			}
		}
	}
}
