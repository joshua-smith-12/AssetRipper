using AsmResolver.DotNet;
using AssetRipper.Assets;
using AssetRipper.Assets.Collections;
using AssetRipper.Assets.Generics;
using AssetRipper.Decompilation.CSharp;
using AssetRipper.Export.Modules.Shaders.IO;
using AssetRipper.Export.UnityProjects.Scripts;
using AssetRipper.Export.UnityProjects.Shaders;
using AssetRipper.GUI.Web.Paths;
using AssetRipper.Import.Structure.Assembly;
using AssetRipper.Import.Structure.Assembly.Managers;
using AssetRipper.Import.Structure.Assembly.Serializable;
using AssetRipper.SourceGenerated.Classes.ClassID_114;
using AssetRipper.SourceGenerated.Classes.ClassID_115;
using AssetRipper.SourceGenerated.Classes.ClassID_48;
using AssetRipper.SourceGenerated.Classes.ClassID_49;
using AssetRipper.SourceGenerated.Extensions;
using AssetRipper.Text.Html;
using System.IO;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AssetRipper.GUI.Web.Pages.Assets;

internal sealed class ObjDataTab : HtmlTab
{
	public string Text { get; }
	public string Text2 { get; }
	public IMonoBehaviour Asset { get; set; }
	public string? FileName { get; }

	public override string DisplayName => "Object Data";

	public override string HtmlName => "objdata";

	public override bool Enabled => !string.IsNullOrEmpty(Text);

	public ObjDataTab(IUnityObjectBase asset)
	{
		Text = TryGetText(asset);
		Text2 = TryGetSecondaryText(asset);
		if (Enabled)
		{
			FileName = GetFileName(asset);
		}
	}

	public override void Write(TextWriter writer)
	{
		new Pre(writer).WithClass("bg-dark-subtle rounded-3 p-2").Close(Text);
		new Pre(writer).WithClass("bg-dark-subtle rounded-3 p-2").Close(Text2);
		using (new Div(writer).WithClass("text-center").End())
		{
			TextSaveButton.Write(writer, FileName, Text);
		}
	}

	public static void Write(TextWriter writer, string? fileName, string? text)
	{
		if (!string.IsNullOrEmpty(text))
		{
			new Pre(writer).WithClass("bg-dark-subtle rounded-3 p-2").Close(text);
			using (new Div(writer).WithClass("text-center").End())
			{
				TextSaveButton.Write(writer, fileName, text);
			}
		}
	}

	public string TryGetText(IUnityObjectBase asset)
	{
		StringBuilder builder = new();
		var behaviour = asset as IMonoBehaviour;
		if (behaviour != null)
		{
			var structure = behaviour.Structure as UnloadedStructure;
			if (structure != null)
			{
				var array = structure.StructureData.ToArray();
				for (var index = 0; index < array.Length; index += 8)
				{
					var max = System.Math.Min(index + 8, array.Length);
					builder.Append(BitConverter.ToString(array[index..max]).Replace("-", " "));
					builder.Append("\n");
				}
				return builder.ToString();
			}
		}

		return "";
	}

	public int ReadInteger(ReadOnlyArraySegment<byte> data, ref int index)
	{
		var result = BitConverter.ToInt32(data.ToArray(), index);
		index += 4;
		return result;
	}

	public float ReadFloat(ReadOnlyArraySegment<byte> data, ref int index)
	{
		var result = BitConverter.ToSingle(data.ToArray(), index);
		index += 4;
		return result;
	}

	public bool ReadBoolean(ReadOnlyArraySegment<byte> data, ref int index)
	{
		var result = ReadInteger(data, ref index);
		return result != 0;
	}

	public string ReadString(ReadOnlyArraySegment<byte> data, ref int index)
	{
		var length = ReadInteger(data, ref index);
		if (length > 0)
		{
			var result = Encoding.UTF8.GetString(data.ToArray(), index, length);
			index += length;
			index += 4 - (index % 4); // round to nearest dword necessary?
			return result;
		}
		return string.Empty;
	}

	public void ResolveReference(ReadOnlyArraySegment<byte> data, StringBuilder builder, ref int index)
	{
		var referenceDword1 = ReadInteger(data, ref index);
		var referenceDword2 = ReadInteger(data, ref index);
		var referenceDword3 = ReadInteger(data, ref index);

		if (referenceDword1 == 0 && referenceDword2 == 0 && referenceDword3 == 0)
		{
			builder.Append("Null\n");
			return;
		}

		if (referenceDword1 == 0 && referenceDword2 != 0)
		{
			var file = Asset.Collection.Assets[referenceDword2];
			if (file != null)
			{
				builder.Append($"Reference -> {Asset.Collection.Name} / {file.ClassName} (FileID={referenceDword2}) (MainAsset={file.MainAsset.GetBestName()})\n");
			}
		}
		else if (referenceDword1 != 0 && referenceDword2 != 0)
		{
			var file = Asset.Collection.Dependencies[referenceDword1].Assets[referenceDword2];
			if (file != null)
			{
				builder.Append($"Reference -> {Asset.Collection.Dependencies[referenceDword1].Name} / {file.ClassName} (MaybeName={Asset.Collection.Dependencies[referenceDword1].Assets[1].GetBestName()}) (FileID={referenceDword2}) (MainAsset={file.MainAsset.GetBestName()})\n");
			}
		}
		else
		{
			builder.Append($"Unidentified reference ({referenceDword1}, {referenceDword2}, {referenceDword3})\n");
		}
	}

	public void DoUIRect(ReadOnlyArraySegment<byte> data, StringBuilder builder, ref int index)
	{
		builder.Append("UIRect:\n");

		builder.Append("\tAnchorPoint leftAnchor:\n");
		builder.Append("\t\tTransform target: ");
		ResolveReference(data, builder, ref index);
		builder.Append($"\t\trelative: {ReadFloat(data, ref index)}\n");
		builder.Append($"\t\tabsolute: {ReadInteger(data, ref index)}\n");

		builder.Append("\tAnchorPoint rightAnchor:\n");
		builder.Append("\t\tTransform target: ");
		ResolveReference(data, builder, ref index);
		builder.Append($"\t\trelative: {ReadFloat(data, ref index)}\n");
		builder.Append($"\t\tabsolute: {ReadInteger(data, ref index)}\n");

		builder.Append("\tAnchorPoint bottomAnchor:\n");
		builder.Append("\t\tTransform target: ");
		ResolveReference(data, builder, ref index);
		builder.Append($"\t\trelative: {ReadFloat(data, ref index)}\n");
		builder.Append($"\t\tabsolute: {ReadInteger(data, ref index)}\n");

		builder.Append("\tAnchorPoint topAnchor:\n");
		builder.Append("\t\tTransform target: ");
		ResolveReference(data, builder, ref index);
		builder.Append($"\t\trelative: {ReadFloat(data, ref index)}\n");
		builder.Append($"\t\tabsolute: {ReadInteger(data, ref index)}\n");

		var updateAnchors = ReadInteger(data, ref index);
		switch (updateAnchors)
		{
			case 0:
				builder.Append($"\tUpdate Anchors: OnEnable\n");
				break;
			case 1:
				builder.Append($"\tUpdate Anchors: OnUpdate\n");
				break;
			case 2:
				builder.Append($"\tUpdate Anchors: OnStart\n");
				break;
			default:
				builder.Append($"\tUpdate Anchors: Unknown {updateAnchors}\n");
				break;
		}
	}

	public void DoUIWidget(ReadOnlyArraySegment<byte> data, StringBuilder builder, ref int index)
	{
		builder.Append("UIWidget:\n");
		builder.Append("\tColor Color:\n");
		builder.Append($"\t\tRed: {ReadFloat(data, ref index)}\n");
		builder.Append($"\t\tGreen: {ReadFloat(data, ref index)}\n");
		builder.Append($"\t\tBlue: {ReadFloat(data, ref index)}\n");
		builder.Append($"\t\tAlpha: {ReadFloat(data, ref index)}\n");

		var pivot = ReadInteger(data, ref index);
		switch (pivot)
		{
			case 0:
				builder.Append($"\tPivot: TopLeft\n");
				break;
			case 1:
				builder.Append($"\tPivot: Top\n");
				break;
			case 2:
				builder.Append($"\tPivot: TopRight\n");
				break;
			case 3:
				builder.Append($"\tPivot: Left\n");
				break;
			case 4:
				builder.Append($"\tPivot: Center\n");
				break;
			case 5:
				builder.Append($"\tPivot: Right\n");
				break;
			case 6:
				builder.Append($"\tPivot: BottomLeft\n");
				break;
			case 7:
				builder.Append($"\tPivot: Bottom\n");
				break;
			case 8:
				builder.Append($"\tPivot: BottomRight\n");
				break;
			default:
				builder.Append($"\tPivot: Unknown {pivot}\n");
				break;
		}

		builder.Append($"\tWidth: {ReadInteger(data, ref index)}\n");
		builder.Append($"\tHeight: {ReadInteger(data, ref index)}\n");
		builder.Append($"\tDepth: {ReadInteger(data, ref index)}\n");

		builder.Append("\tMaterial material: ");
		ResolveReference(data, builder, ref index);

		builder.Append($"\tAuto-resize Collider: {ReadBoolean(data, ref index)}\n");
		builder.Append($"\tHide if Off Screen: {ReadBoolean(data, ref index)}\n");

		var aspectRatioSource = ReadInteger(data, ref index);
		switch (aspectRatioSource)
		{
			case 0:
				builder.Append($"\tAspect Ratio Source: Free\n");
				break;
			case 1:
				builder.Append($"\tAspect Ratio Source: Width\n");
				break;
			case 2:
				builder.Append($"\tAspect Ratio Source: Height\n");
				break;
			default:
				builder.Append($"\tAspect Ratio Source: Unknown {aspectRatioSource}\n");
				break;
		}

		builder.Append($"\tAspect Ratio: {ReadFloat(data, ref index)}\n");

	}

	public void DoUIBasicSprite(ReadOnlyArraySegment<byte> data, StringBuilder builder, ref int index)
	{
		builder.Append("UIBasicSprite:\n");

		var spriteType = ReadInteger(data, ref index);
		switch (spriteType)
		{
			case 0:
				builder.Append($"\tSprite Type: Simple\n");
				break;
			case 1:
				builder.Append($"\tSprite Type: Sliced\n");
				break;
			case 2:
				builder.Append($"\tSprite Type: Tiled\n");
				break;
			case 3:
				builder.Append($"\tSprite Type: Filled\n");
				break;
			case 4:
				builder.Append($"\tSprite Type: Advanced\n");
				break;
			default:
				builder.Append($"\tSprite Type: Unknown {spriteType}\n");
				break;
		}

		var fillDirection = ReadInteger(data, ref index);
		switch (fillDirection)
		{
			case 0:
				builder.Append($"\tFill Direction: Horizontal\n");
				break;
			case 1:
				builder.Append($"\tFill Direction: Vertical\n");
				break;
			case 2:
				builder.Append($"\tFill Direction: Radial 90\n");
				break;
			case 3:
				builder.Append($"\tFill Direction: Radial 180\n");
				break;
			case 4:
				builder.Append($"\tFill Direction: Radial 360\n");
				break;
			default:
				builder.Append($"\tFill Direction: Unknown {fillDirection}\n");
				break;
		}

		builder.Append($"\tFill Amount: {ReadFloat(data, ref index)}\n");
		builder.Append($"\tInverted: {ReadBoolean(data, ref index)}\n");

		var flip = ReadInteger(data, ref index);
		switch (flip)
		{
			case 0:
				builder.Append($"\tFlip: Nothing\n");
				break;
			case 1:
				builder.Append($"\tFlip: Horizontal\n");
				break;
			case 2:
				builder.Append($"\tFlip: Vertical\n");
				break;
			case 3:
				builder.Append($"\tFlip: Both\n");
				break;
			default:
				builder.Append($"\tFlip: Unknown {flip}\n");
				break;
		}

		builder.Append($"\tApply Gradient: {ReadBoolean(data, ref index)}\n");

		builder.Append("\tColor Top Gradient:\n");
		builder.Append($"\t\tRed: {ReadFloat(data, ref index)}\n");
		builder.Append($"\t\tGreen: {ReadFloat(data, ref index)}\n");
		builder.Append($"\t\tBlue: {ReadFloat(data, ref index)}\n");
		builder.Append($"\t\tAlpha: {ReadFloat(data, ref index)}\n");

		builder.Append("\tColor Bottom Gradient:\n");
		builder.Append($"\t\tRed: {ReadFloat(data, ref index)}\n");
		builder.Append($"\t\tGreen: {ReadFloat(data, ref index)}\n");
		builder.Append($"\t\tBlue: {ReadFloat(data, ref index)}\n");
		builder.Append($"\t\tAlpha: {ReadFloat(data, ref index)}\n");

		var advancedType = ReadInteger(data, ref index);
		switch (advancedType)
		{
			case 0:
				builder.Append($"\tAdvanced Type Center: Invisible\n");
				break;
			case 1:
				builder.Append($"\tAdvanced Type Center: Invisible\n");
				break;
			case 2:
				builder.Append($"\tAdvanced Type Center: Invisible\n");
				break;
			default:
				builder.Append($"\tAdvanced Type Center: Unknown {advancedType}\n");
				break;
		}

		advancedType = ReadInteger(data, ref index);
		switch (advancedType)
		{
			case 0:
				builder.Append($"\tAdvanced Type Left: Invisible\n");
				break;
			case 1:
				builder.Append($"\tAdvanced Type Left: Invisible\n");
				break;
			case 2:
				builder.Append($"\tAdvanced Type Left: Invisible\n");
				break;
			default:
				builder.Append($"\tAdvanced Type Left: Unknown {advancedType}\n");
				break;
		}

		advancedType = ReadInteger(data, ref index);
		switch (advancedType)
		{
			case 0:
				builder.Append($"\tAdvanced Type Right: Invisible\n");
				break;
			case 1:
				builder.Append($"\tAdvanced Type Right: Invisible\n");
				break;
			case 2:
				builder.Append($"\tAdvanced Type Right: Invisible\n");
				break;
			default:
				builder.Append($"\tAdvanced Type Right: Unknown {advancedType}\n");
				break;
		}

		advancedType = ReadInteger(data, ref index);
		switch (advancedType)
		{
			case 0:
				builder.Append($"\tAdvanced Type Bottom: Invisible\n");
				break;
			case 1:
				builder.Append($"\tAdvanced Type Bottom: Invisible\n");
				break;
			case 2:
				builder.Append($"\tAdvanced Type Bottom: Invisible\n");
				break;
			default:
				builder.Append($"\tAdvanced Type Bottom: Unknown {advancedType}\n");
				break;
		}

		advancedType = ReadInteger(data, ref index);
		switch (advancedType)
		{
			case 0:
				builder.Append($"\tAdvanced Type Top: Invisible\n");
				break;
			case 1:
				builder.Append($"\tAdvanced Type Top: Invisible\n");
				break;
			case 2:
				builder.Append($"\tAdvanced Type Top: Invisible\n");
				break;
			default:
				builder.Append($"\tAdvanced Type Top: Unknown {advancedType}\n");
				break;
		}
	}

	public void DoUISprite(ReadOnlyArraySegment<byte> data, StringBuilder builder, ref int index)
	{
		builder.Append("UISprite:\n");

		builder.Append("\tObject Atlas: ");
		ResolveReference(data, builder, ref index);

		builder.Append($"\tSprite Name: {ReadString(data, ref index)}\n");
		builder.Append($"\tFixed Aspect Ratio: {ReadBoolean(data, ref index)}\n");
		builder.Append($"\tFill Center: {ReadBoolean(data, ref index)}\n");
	}

	public void DoUILabel(ReadOnlyArraySegment<byte> data, StringBuilder builder, ref int index)
	{

	}

	public void DoUICamera(ReadOnlyArraySegment<byte> data, StringBuilder builder, ref int index)
	{
		builder.Append("UICamera:\n");
		var eventType = ReadInteger(data, ref index);
		switch (eventType)
		{
			case 0:
				builder.Append($"\tEvent Type: World 3D\n");
				break;
			case 1:
				builder.Append($"\tEvent Type: UI 3D\n");
				break;
			case 2:
				builder.Append($"\tEvent Type: World 2D\n");
				break;
			case 3:
				builder.Append($"\tEvent Type: UI 2D\n");
				break;
			default:
				builder.Append($"\tEvent Type: Unknown {eventType}\n");
				break;
		}

		builder.Append($"\tEvents Go to Colliders: {ReadBoolean(data, ref index)}\n");
		builder.Append($"\tLayer Mask: {ReadInteger(data, ref index)}\n");

		var processEventsIn = ReadInteger(data, ref index);
		switch (processEventsIn)
		{
			case 0:
				builder.Append($"\tProcess Events In: Update\n");
				break;
			case 1:
				builder.Append($"\tProcess Events In: LateUpdate\n");
				break;
			default:
				builder.Append($"\tEvent Type: Unknown {processEventsIn}\n");
				break;
		}

		builder.Append($"\tDebug: {ReadBoolean(data, ref index)}\n");
		builder.Append($"\tUse Mouse: {ReadBoolean(data, ref index)}\n");
		builder.Append($"\tUse Touch: {ReadBoolean(data, ref index)}\n");
		builder.Append($"\tAllow Multi Touch: {ReadBoolean(data, ref index)}\n");
		builder.Append($"\tUse Keyboard: {ReadBoolean(data, ref index)}\n");
		builder.Append($"\tUse Controller: {ReadBoolean(data, ref index)}\n");
		builder.Append($"\tSticky Tooltip: {ReadBoolean(data, ref index)}\n");
		builder.Append($"\tTooltip Delay: {ReadFloat(data, ref index)}\n");
		builder.Append($"\tLong Press Tooltip: {ReadBoolean(data, ref index)}\n");
		builder.Append($"\tMouse Drag Threshold: {ReadFloat(data, ref index)}\n");
		builder.Append($"\tMouse Click Threshold: {ReadFloat(data, ref index)}\n");
		builder.Append($"\tTouch Drag Threshold: {ReadFloat(data, ref index)}\n");
		builder.Append($"\tTouch Click Threshold: {ReadFloat(data, ref index)}\n");
		builder.Append($"\tRange Distance: {ReadFloat(data, ref index)}\n");
		builder.Append($"\tHorizontal Axis Name: {ReadString(data, ref index)}\n");
		builder.Append($"\tVertical Axis Name: {ReadString(data, ref index)}\n");
		builder.Append($"\tHorizontal Pan Axis Name: {ReadString(data, ref index)}\n");
		builder.Append($"\tVertical Pan Axis Name: {ReadString(data, ref index)}\n");
		builder.Append($"\tScroll Axis Name: {ReadString(data, ref index)}\n");
		builder.Append($"\tCommand Click: {ReadBoolean(data, ref index)}\n");
		builder.Append($"\tSubmit Key 0: {ReadInteger(data, ref index)}\n");
		builder.Append($"\tSubmit Key 1: {ReadInteger(data, ref index)}\n");
		builder.Append($"\tCancel Key 0: {ReadInteger(data, ref index)}\n");
		builder.Append($"\tCancel Key 1: {ReadInteger(data, ref index)}\n");
		builder.Append($"\tAuto Hide Cursor: {ReadBoolean(data, ref index)}\n");
	}

	public void DoUIWidgetContainer(ReadOnlyArraySegment<byte> data, StringBuilder builder, ref int index)
	{

	}

	public void DoUIButtonColor(ReadOnlyArraySegment<byte> data, StringBuilder builder, ref int index)
	{

	}

	public void DoUIButton(ReadOnlyArraySegment<byte> data, StringBuilder builder, ref int index)
	{

	}

	public void DoUIViewport(ReadOnlyArraySegment<byte> data, StringBuilder builder, ref int index)
	{

	}

	public void DoUIRoot(ReadOnlyArraySegment<byte> data, StringBuilder builder, ref int index)
	{
		builder.Append("UIRoot:\n");
		var scaling = ReadInteger(data, ref index);
		builder.Append($"\tScaling Style: ");
		switch (scaling)
		{
			case 0:
				builder.Append("Pixel Perfect\n");
				break;
			case 1:
				builder.Append("Fixed Size\n");
				break;
			case 2:
				builder.Append("Fixed Size on Mobile\n");
				break;
			default:
				builder.Append($"Unknown {scaling}\n");
				break;
		}
		builder.Append($"\tManual Width: {ReadInteger(data, ref index)}\n");
		builder.Append($"\tManual Height: {ReadInteger(data, ref index)}\n");
		builder.Append($"\tMinimum Height: {ReadInteger(data, ref index)}\n");
		builder.Append($"\tMaximum Height: {ReadInteger(data, ref index)}\n");
		builder.Append($"\tFit Width: {ReadBoolean(data, ref index)}\n");
		builder.Append($"\tFit Height: {ReadBoolean(data, ref index)}\n");
		builder.Append($"\tAdjust by DPI: {ReadBoolean(data, ref index)}\n");
		builder.Append($"\tShrink Portrait UI: {ReadBoolean(data, ref index)}\n");
	}

	public void DoUIPanel(ReadOnlyArraySegment<byte> data, StringBuilder builder, ref int index)
	{
		builder.Append("UIPanel:\n");
		builder.Append($"\tShow In Panel Tool: {ReadBoolean(data, ref index)}\n");
		builder.Append($"\tGenerate Normals: {ReadBoolean(data, ref index)}\n");
		builder.Append($"\tGenerate UV2: {ReadBoolean(data, ref index)}\n");
		var shadowMode = ReadInteger(data, ref index);
		switch(shadowMode)
		{
			case 0:
				builder.Append($"\tShadow Mode: None\n");
				break;
			case 1:
				builder.Append($"\tShadow Mode: Receive\n");
				break;
			case 2:
				builder.Append($"\tShadow Mode: Cast and Receive\n");
				break;
			default:
				builder.Append($"\tShadow Mode: Unknown {shadowMode}\n");
				break;
		}
		builder.Append($"\tStatic Widgets: {ReadBoolean(data, ref index)}\n");
		builder.Append($"\tCull while Dragging: {ReadBoolean(data, ref index)}\n");
		builder.Append($"\tAlways on Screen: {ReadBoolean(data, ref index)}\n");
		builder.Append($"\tAnchor Offset: {ReadBoolean(data, ref index)}\n");
		builder.Append($"\tSoft Border Padding: {ReadBoolean(data, ref index)}\n");
		var renderQueue = ReadInteger(data, ref index);
		switch (renderQueue)
		{
			case 0:
				builder.Append($"\tRender Queue: Automatic\n");
				break;
			case 1:
				builder.Append($"\tRender Queue: Start At\n");
				break;
			case 2:
				builder.Append($"\tRender Queue: Explicit\n");
				break;
			default:
				builder.Append($"\tRender Queue: Unknown {renderQueue}\n");
				break;
		}
		builder.Append($"\tStarting Render Queue: {ReadInteger(data, ref index)}\n");
		builder.Append($"\tTexture2D Clip Texture: ");
		ResolveReference(data, builder, ref index);
		builder.Append($"\tAlpha: {ReadFloat(data, ref index)}\n");
		var clipping = ReadInteger(data, ref index);
		switch (clipping)
		{
			case 0:
				builder.Append($"\tClipping: None\n");
				break;
			case 1:
				builder.Append($"\tClipping: Texture Mask\n");
				break;
			case 3:
				builder.Append($"\tClipping: Soft Clip\n");
				break;
			case 4:
				builder.Append($"\tClipping: Constrain but Don't Clip\n");
				break;
			default:
				builder.Append($"\tClipping: Unknown {clipping}\n");
				break;
		}
		builder.Append($"\tVector4 Clip Range:\n");
		builder.Append($"\t\tunk0: {ReadFloat(data, ref index)}\n");
		builder.Append($"\t\tunk1: {ReadFloat(data, ref index)}\n");
		builder.Append($"\t\tunk2: {ReadFloat(data, ref index)}\n");
		builder.Append($"\t\tunk3: {ReadFloat(data, ref index)}\n");
		builder.Append($"\tVector2 Clip Softness:\n");
		builder.Append($"\t\tunk0: {ReadFloat(data, ref index)}\n");
		builder.Append($"\t\tunk1: {ReadFloat(data, ref index)}\n");
		builder.Append($"\tDepth: {ReadInteger(data, ref index)}\n");
		builder.Append($"\tSorting Order: {ReadInteger(data, ref index)}\n");
		builder.Append($"\tSorting Layer Name: {ReadString(data, ref index)}\n");
		builder.Append($"\tVector2 Clip Offset:\n");
		builder.Append($"\t\tunk0: {ReadFloat(data, ref index)}\n");
		builder.Append($"\t\tunk1: {ReadFloat(data, ref index)}\n");
		builder.Append($"\tUse Sorting Order: {ReadBoolean(data, ref index)}\n");
	}

	public void DoUIAtlas(ReadOnlyArraySegment<byte> data, StringBuilder builder, ref int index)
	{
		builder.Append("UIAtlas:\n");

		builder.Append("\tMaterial Material: ");
		ResolveReference(data, builder, ref index);

		var spriteCount = ReadInteger(data, ref index);
		builder.Append($"\tList<SpriteData> Sprites ({spriteCount}):\n");
		while(spriteCount > 0)
		{
			builder.Append($"\t\tName: {ReadString(data, ref index)}\n");
			builder.Append($"\t\t\tPosition: ({ReadInteger(data, ref index)}, {ReadInteger(data, ref index)})\n");
			builder.Append($"\t\t\tWidth: {ReadInteger(data, ref index)}\n");
			builder.Append($"\t\t\tHeight: {ReadInteger(data, ref index)}\n");
			builder.Append($"\t\t\tLeft Border: {ReadInteger(data, ref index)}\n");
			builder.Append($"\t\t\tRight Border: {ReadInteger(data, ref index)}\n");
			builder.Append($"\t\t\tTop Border: {ReadInteger(data, ref index)}\n");
			builder.Append($"\t\t\tBottom Border: {ReadInteger(data, ref index)}\n");
			builder.Append($"\t\t\tLeft Padding: {ReadInteger(data, ref index)}\n");
			builder.Append($"\t\t\tRight Padding: {ReadInteger(data, ref index)}\n");
			builder.Append($"\t\t\tTop Padding: {ReadInteger(data, ref index)}\n");
			builder.Append($"\t\t\tBottom Padding: {ReadInteger(data, ref index)}\n");
			spriteCount--;
		}

		builder.Append($"\tPixel Size: {ReadFloat(data, ref index)}\n");

		builder.Append("\tObject Replacement: ");
		ResolveReference(data, builder, ref index);

		var coords = ReadInteger(data, ref index);
		switch (coords)
		{
			case 0:
				builder.Append($"\tCo-ordinates: Pixels\n");
				break;
			case 1:
				builder.Append($"\tCo-ordinates: Texture Co-Ordinates\n");
				break;
			default:
				builder.Append($"\tCo-ordinates: Unknown {coords}\n");
				break;
		}

		builder.Append($"\tFake sprite count: {ReadInteger(data, ref index)}");
	}

	public string TryGetSecondaryText(IUnityObjectBase asset)
	{
		StringBuilder builder = new();
		var behaviour = asset as IMonoBehaviour;
		if (behaviour != null)
		{
			Asset = behaviour;
			var structure = behaviour.Structure as UnloadedStructure;
			if (structure != null)
			{
				var index = 0;
				switch(behaviour.ScriptP?.ClassName_R)
				{
					case "UIRect":
						DoUIRect(structure.StructureData, builder, ref index);
						break;
					case "UIWidget":
						DoUIRect(structure.StructureData, builder, ref index);
						DoUIWidget(structure.StructureData, builder, ref index);
						break;
					case "UIBasicSprite":
						DoUIRect(structure.StructureData, builder, ref index);
						DoUIWidget(structure.StructureData, builder, ref index);
						DoUIBasicSprite(structure.StructureData, builder, ref index);
						break;
					case "UISprite":
						DoUIRect(structure.StructureData, builder, ref index);
						DoUIWidget(structure.StructureData, builder, ref index);
						DoUIBasicSprite(structure.StructureData, builder, ref index);
						DoUISprite(structure.StructureData, builder, ref index);
						break;
					case "UILabel":
						DoUIRect(structure.StructureData, builder, ref index);
						DoUIWidget(structure.StructureData, builder, ref index);
						DoUILabel(structure.StructureData, builder, ref index);
						break;
					case "UIButton":
						DoUIWidgetContainer(structure.StructureData, builder, ref index);
						DoUIButtonColor(structure.StructureData, builder, ref index);
						DoUIButton(structure.StructureData, builder, ref index);
						break;
					case "UIButtonColor":
						DoUIWidgetContainer(structure.StructureData, builder, ref index);
						DoUIButtonColor(structure.StructureData, builder, ref index);
						break;
					case "UIWidgetContainer":
						DoUIWidgetContainer(structure.StructureData, builder, ref index);
						break;
					case "UIAtlas":
						DoUIAtlas(structure.StructureData, builder, ref index);
						break;
					case "UIViewport":
						DoUIViewport(structure.StructureData, builder, ref index);
						break;
					case "UIRoot":
						DoUIRoot(structure.StructureData, builder, ref index);
						break;
					case "UIPanel":
						DoUIRect(structure.StructureData, builder, ref index);
						DoUIPanel(structure.StructureData, builder, ref index);
						break;
					case "UICamera":
						DoUICamera(structure.StructureData, builder, ref index);
						break;
					default:
						var array = structure.StructureData.ToArray();
						for (index = 0; index < array.Length; index += 8)
						{
							var max = System.Math.Min(index + 8, array.Length);
							for (var b = index; b < max; b++)
							{
								if (array[b] >= 32 && array[b] <= 126) builder.Append(Encoding.ASCII.GetString(new[] { array[b] }) + " ");
								else builder.Append("   ");
							}
							builder.Append("\n");
						}
						break;
				}
				
				return builder.ToString();
			}
		}

		return "";
	}

	public static string GetFileName(IUnityObjectBase asset)
	{
		return asset switch
		{
			IShader => $"{asset.GetBestName()}.shader",
			IMonoScript monoScript => $"{monoScript.ClassName_R}.cs",
			ITextAsset textAsset => $"{asset.GetBestName()}.{GetTextAssetExtension(textAsset)}",
			_ => $"{asset.GetBestName()}.txt",
		};

		static string GetTextAssetExtension(ITextAsset textAsset)
		{
			return string.IsNullOrEmpty(textAsset.OriginalExtension) ? "txt" : textAsset.OriginalExtension;
		}
	}
}
