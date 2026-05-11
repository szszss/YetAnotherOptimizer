using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches.ThreadSafe
{
	[HarmonyPatch(typeof(Room))]
	[HarmonyPatch(nameof(Room.Regions), MethodType.Getter)]
	internal static class Verse_Room_Regions
	{
		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe;
		}

		// Old implementation: It fails when a thread uses two Room.Regions simultaneously.
		/*
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			return ThreadLocalHelper.ThreadLocalTranspiler(instructions, generator, "tmpRegions");
		}
		*/

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var localList = generator.DeclareLocal(typeof(List<Region>));
			var fieldList = AccessTools.Field(typeof(Room), "tmpRegions");
			// var localList = TransientPool.BorrowIfNotMainThread(this.tmpRegions);
			yield return CodeInstruction.LoadArgument(0);
			yield return CodeInstruction.LoadField(typeof(Room), "tmpRegions");
			yield return CodeInstruction.Call(
				typeof(TransientPool<List<Region>>),
				nameof(TransientPool<List<Region>>.BorrowIfNotMainThread));
			yield return CodeInstruction.StoreLocal(localList.LocalIndex);

			foreach (var instruction in instructions)
			{
				// replace this.tmpRegions with localList
				if (instruction.LoadsField(fieldList))
				{
					yield return new CodeInstruction(OpCodes.Pop);
					yield return CodeInstruction.LoadLocal(localList.LocalIndex);
					continue;
				}
				yield return instruction;
			}
		}
	}
}