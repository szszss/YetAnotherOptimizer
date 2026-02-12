using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;

namespace YaOpt.Patches
{
	/// <summary>
	/// <seealso cref="YaOptSettings.OptParallelPawnTick"/>
	/// </summary>
	[HarmonyPatch(typeof(TickList))]
	[HarmonyPatch("BucketOf")]
	internal static class Verse_TickList_BucketOf
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelPawnTick.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			/*
			 *if (this.tickType == TickerType.Normal && t is Pawn)
			 * {
			 * 	return this.thingLists[1];
			 * }
			 */
			var label = generator.DefineLabel();
			yield return CodeInstruction.LoadArgument(0);
			yield return CodeInstruction.LoadField(typeof(TickList), "tickType");
			yield return new CodeInstruction(OpCodes.Ldc_I4_1); // TickerType.Normal
			yield return new CodeInstruction(OpCodes.Bne_Un_S, label);
			yield return CodeInstruction.LoadArgument(1);
			yield return new CodeInstruction(OpCodes.Isinst, typeof(Pawn));
			yield return new CodeInstruction(OpCodes.Brfalse_S, label);
			yield return CodeInstruction.LoadArgument(0);
			yield return CodeInstruction.LoadField(typeof(TickList), "thingLists");
			yield return new CodeInstruction(OpCodes.Ldc_I4_1);
			yield return new CodeInstruction(OpCodes.Callvirt,
				AccessTools.Method(typeof(List<List<Thing>>), "get_Item"));
			yield return new CodeInstruction(OpCodes.Ret);
			var firstOp = true;
			foreach (var instruction in instructions)
			{
				if (firstOp)
				{
					firstOp = false;
					instruction.labels.Add(label);
				}
				yield return instruction;
			}
		}
	}
}