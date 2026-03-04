using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;
using YaOpt.Helpers.ThreadLocal;

namespace YaOpt.Patches.ThreadSafe.ThreadLocal
{
	// TODO: This is not a robust fix. It fails when a thread uses two Room.Regions simultaneously.
	[HarmonyPatch(typeof(Room))]
	[HarmonyPatch(nameof(Room.Regions), MethodType.Getter)]
	internal static class Verse_Room_Regions
	{
		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			return ThreadLocalHelper.ThreadLocalTranspiler(instructions, generator, "tmpRegions");
		}
	}
}