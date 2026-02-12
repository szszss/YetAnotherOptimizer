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
	[HarmonyPatch(nameof(MapDrawer.MapMeshDrawerUpdate_First))]
	internal static class Verse_MapDrawer_MapMeshDrawerUpdate_First
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelRenderPrepare.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var typeThingCullDetails = AccessTools.TypeByName("Verse.DynamicDrawManager/ThingCullDetails");
			var typeNativeArrayThingCullDetails = typeof(NativeArray<>).MakeGenericType(typeThingCullDetails);
			var local = generator.DeclareLocal(typeNativeArrayThingCullDetails);
			// var nativeArray = new NativeArray<DynamicDrawManager.ThingCullDetails>(
			//		Find.CurrentMap.dynamicDrawManager.drawThings.Count, Allocator.TempJob, NativeArrayOptions.ClearMemory);
			yield return CodeInstruction.LoadLocal(local.LocalIndex, true);
			yield return new CodeInstruction(OpCodes.Call,
				AccessTools.PropertyGetter(typeof(Find), nameof(Find.CurrentMap)));
			yield return CodeInstruction.LoadField(typeof(Map), nameof(Map.dynamicDrawManager));
			yield return CodeInstruction.LoadField(typeof(DynamicDrawManager), "drawThings");
			yield return new CodeInstruction(OpCodes.Callvirt,
				AccessTools.PropertyGetter(typeof(List<Thing>), "Count"));
			yield return new CodeInstruction(OpCodes.Ldc_I4_3);
			yield return new CodeInstruction(OpCodes.Ldc_I4_1);
			yield return new CodeInstruction(OpCodes.Call,
				AccessTools.Constructor(typeNativeArrayThingCullDetails,
					new[] { typeof(int), typeof(Allocator), typeof(NativeArrayOptions) }));

			// ParallelPreDrawHelper.Data = (object)nativeArray;
			yield return CodeInstruction.LoadLocal(local.LocalIndex);
			yield return new CodeInstruction(OpCodes.Box, typeNativeArrayThingCullDetails);
			yield return CodeInstruction.StoreField(
				typeof(ParallelPreDrawHelper),
				nameof(ParallelPreDrawHelper.Data));
			// Find.CurrentMap.dynamicDrawManager.ComputeCulledThings(nativeArray)
			yield return new CodeInstruction(OpCodes.Call,
				AccessTools.PropertyGetter(typeof(Find), nameof(Find.CurrentMap)));
			yield return CodeInstruction.LoadField(typeof(Map), nameof(Map.dynamicDrawManager));
			yield return CodeInstruction.LoadLocal(local.LocalIndex);
			yield return CodeInstruction.Call(typeof(DynamicDrawManager), "ComputeCulledThings");

			foreach (var instruction in instructions)
			{
				yield return instruction;
			}
		}
	}
}