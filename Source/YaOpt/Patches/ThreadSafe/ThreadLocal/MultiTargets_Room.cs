using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using YaOpt.Helpers;
using YaOpt.Helpers.ThreadLocal;

namespace YaOpt.Patches.ThreadSafe.ThreadLocal
{
	// Not really safe. It will fail when iterating through ContainedThings in two rooms simultaneously.
	[HarmonyPatch]
	internal static class MultiTargets_Room
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(Room), nameof(Room.ThingCount));
			foreach (var nestedType in typeof(Room).GetNestedTypes(
						 BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic))
			{
				// Can't patch generic
				if (nestedType.IsGenericType)
					continue;
				foreach (var method in nestedType.GetMethods(
					BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
				{
					if (nestedType.Name.Contains("<ContainedThings>") &&
						method.Name == "MoveNext")
					{
						YaOptMod.Debug($"MultiTargets_Room found a method from Room: {method.FullName()}");
						yield return method;
					}
				}
			}
		}

		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			return ThreadLocalHelper.ThreadLocalTranspiler(instructions, generator, "uniqueContainedThingsOfDef");
		}
	}
}
