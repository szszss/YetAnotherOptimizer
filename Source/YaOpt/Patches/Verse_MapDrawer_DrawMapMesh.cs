using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using Unity.Collections;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <summary>
	/// <seealso cref="YaOptSettings.OptParallelRenderPrepare"/>
	/// </summary>
	[HarmonyPatch(typeof(MapDrawer))]
	[HarmonyPatch(nameof(MapDrawer.DrawMapMesh))]
	internal static class Verse_MapDrawer_DrawMapMesh
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelRenderPrepare.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var typeThingCullDetails = AccessTools.TypeByName("Verse.DynamicDrawManager/ThingCullDetails");
			var typeNativeArrayThingCullDetails = typeof(NativeArray<>).MakeGenericType(typeThingCullDetails);
			var localNativeArray = generator.DeclareLocal(typeNativeArrayThingCullDetails);
			var localThings = generator.DeclareLocal(typeof(List<Thing>));
			var localLoop = generator.DeclareLocal(typeof(int));
			var localLength = generator.DeclareLocal(typeof(int));
			var labelLoopBegin = generator.DefineLabel();
			var labelShouldNotDraw = generator.DefineLabel();
			// ParallelPreDrawHelper.WaitUntilCullJobComplete()
			yield return CodeInstruction.Call(typeof(ParallelPreDrawHelper), 
				nameof(ParallelPreDrawHelper.WaitUntilCullJobComplete));
			// var nativeArray = (NativeArray<DynamicDrawManager.ThingCullDetails>)ParallelPreDrawHelper.Data
			yield return CodeInstruction.LoadField(
				typeof(ParallelPreDrawHelper), 
				nameof(ParallelPreDrawHelper.Data));
			yield return new CodeInstruction(OpCodes.Unbox_Any, typeNativeArrayThingCullDetails);
			yield return CodeInstruction.StoreLocal(localNativeArray.LocalIndex);
			// var things = Find.CurrentMap.dynamicDrawManager.drawThings
			yield return new CodeInstruction(OpCodes.Call, 
				AccessTools.PropertyGetter(typeof(Find), nameof(Find.CurrentMap)));
			yield return CodeInstruction.LoadField(typeof(Map), nameof(Map.dynamicDrawManager));
			yield return CodeInstruction.LoadField(typeof(DynamicDrawManager), "drawThings");
			yield return CodeInstruction.StoreLocal(localThings.LocalIndex);
			// int i = 0
			yield return new CodeInstruction(OpCodes.Ldc_I4_0);
			yield return CodeInstruction.StoreLocal(localLoop.LocalIndex);
			// int j = nativeArray.length
			yield return CodeInstruction.LoadLocal(localNativeArray.LocalIndex, true);
			yield return new CodeInstruction(OpCodes.Call, 
				AccessTools.PropertyGetter(typeNativeArrayThingCullDetails, "Length"));
			yield return CodeInstruction.StoreLocal(localLength.LocalIndex);
			// labelLoopBegin:
			// if (!nativeArray[i].shouldDraw) goto labelShouldNotDraw
			yield return CodeInstruction.LoadLocal(localNativeArray.LocalIndex, true).WithLabels(labelLoopBegin);
			yield return CodeInstruction.LoadLocal(localLoop.LocalIndex);
			yield return new CodeInstruction(OpCodes.Call, 
				AccessTools.Method(typeNativeArrayThingCullDetails, "get_Item"));
			yield return CodeInstruction.LoadField(typeThingCullDetails, "shouldDraw");
			yield return new CodeInstruction(OpCodes.Brfalse_S, labelShouldNotDraw);
			// drawThings[i].DynamicDrawPhase(DrawPhase.EnsureInitialized);
			yield return CodeInstruction.LoadLocal(localThings.LocalIndex);
			yield return CodeInstruction.LoadLocal(localLoop.LocalIndex);
			yield return new CodeInstruction(OpCodes.Callvirt, 
				AccessTools.Method(typeof(List<Thing>), "get_Item"));
			yield return new CodeInstruction(OpCodes.Ldc_I4_0);
			yield return new CodeInstruction(OpCodes.Callvirt, 
				AccessTools.Method(typeof(Thing), nameof(Thing.DynamicDrawPhase)));
			// labelShouldNotDraw:
			// i++
			yield return CodeInstruction.LoadLocal(localLoop.LocalIndex).WithLabels(labelShouldNotDraw);
			yield return new CodeInstruction(OpCodes.Ldc_I4_1);
			yield return new CodeInstruction(OpCodes.Add);
			yield return new CodeInstruction(OpCodes.Dup);
			yield return CodeInstruction.StoreLocal(localLoop.LocalIndex);
			// if (i < j) goto labelLoopBegin
			yield return CodeInstruction.LoadLocal(localLength.LocalIndex);
			yield return new CodeInstruction(OpCodes.Blt_S, labelLoopBegin);
			// Find.CurrentMap.dynamicDrawManager.PreDrawVisibleThings(nativeArray);
			yield return new CodeInstruction(OpCodes.Call, 
				AccessTools.PropertyGetter(typeof(Find), nameof(Find.CurrentMap)));
			yield return CodeInstruction.LoadField(typeof(Map), nameof(Map.dynamicDrawManager));
			yield return CodeInstruction.LoadLocal(localNativeArray.LocalIndex);
			yield return CodeInstruction.Call(typeof(DynamicDrawManager), "PreDrawVisibleThings");

			foreach (var instruction in instructions)
			{
				yield return instruction;
			}
		}
	}
}