using HarmonyLib;
using System.Reflection;
using YaOpt.Helpers.ThreadSafe;

namespace YaOpt.Patches.Compatibility.VehicleMapFramework
{
	[HarmonyPatch]
	internal static class VehicleMapFramework_CrossMapMapPawnsCache_Get
	{
		static MethodBase TargetMethod()
		{
			VehicleMapFrameworkCompatibility.Init();
			return AccessTools.Method(
				AccessTools.TypeByName("VehicleMapFramework.CrossMapMapPawnsCache"),
				"Get");
		}

		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe &&
				   YaOptGlobal.HasMod("oels.vehiclemapframework");
		}

		static void Prefix(object __instance, out bool __state)
		{
			__state = false;
			GreedyMonitor.Enter(__instance, ref __state);
		}

		static void Finalizer(object __instance, bool __state)
		{
			if (__state)
				GreedyMonitor.Exit(__instance);
		}

		/*static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var local = generator.DeclareLocal(typeof(List<Pawn>));

			// var list = ThreadLocalMapPawns.GetPooledList();
			yield return CodeInstruction.Call(
				typeof(ThreadLocalMapPawns),
				nameof(ThreadLocalMapPawns.GetPooledList));
			yield return CodeInstruction.StoreLocal(local.LocalIndex);

			foreach (var instruction in instructions)
			{
				if (instruction.LoadsField("cachedPawns"))
				{
					yield return new CodeInstruction(OpCodes.Pop);
					yield return CodeInstruction.LoadLocal(local.LocalIndex);
					continue;
				}
				yield return instruction;
			}
		}*/
	}
}