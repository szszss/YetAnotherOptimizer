using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <seealso cref="YaOptSettings.OptGetMapPawns"/>
	[HarmonyPatch(typeof(ThingOwnerUtility))]
	[HarmonyPatch(nameof(ThingOwnerUtility.GetAllThingsRecursively))]
	[HarmonyPatch(new[] { typeof(IThingHolder), typeof(List<Thing>), typeof(bool), typeof(Predicate<IThingHolder>) })]
	internal static class Verse_ThingOwnerUtility_GetAllThingsRecursively
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptGetMapPawns.Enabled;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void TryAddRange(List<Thing> list, ThingOwner thingOwner)
		{
			if (thingOwner.Any)
				list.AddRange(thingOwner);
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			foreach (var instruction in instructions)
			{
				if (instruction.Calls("AddRange"))
				{
					yield return CodeInstruction.Call(
						typeof(Verse_ThingOwnerUtility_GetAllThingsRecursively),
						nameof(TryAddRange));
					continue;
				}
				yield return instruction;
			}
		}
	}
}