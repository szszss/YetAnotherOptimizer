using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches.ThreadSafe
{
	[HarmonyPatch]
	internal static class MultiTargets_PoolReplace
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(JobMaker), nameof(JobMaker.MakeJob), Type.EmptyTypes);
			yield return AccessTools.Method(typeof(JobMaker), nameof(JobMaker.ReturnToPool));
			yield return AccessTools.Method(typeof(ToilMaker), nameof(ToilMaker.MakeToil));
			yield return AccessTools.Method(typeof(ToilMaker), nameof(ToilMaker.ReturnToPool));
			yield return AccessTools.Method(typeof(GenClosest), nameof(GenClosest.RegionwiseBFSWorker));
			foreach (var method in GetPawnRelationsTracker())
				yield return method;
		}

		private static IEnumerable<MethodBase> GetPawnRelationsTracker()
		{
			var type = typeof(Pawn_RelationsTracker);
			foreach (var nested in type.GetNestedTypes(BindingFlags.NonPublic))
			{
				if (nested.GetField("<>1__state", BindingFlags.NonPublic | BindingFlags.Instance) == null)
					continue;
				foreach (var method in nested.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
				{
					if (method.Name == "Reset") continue;
					yield return method;
				}
			}
		}

		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe || YaOptGlobal.Settings.OptParallelWorkGiver.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				// from SimplePool<T>.Get()/Return(obj)/FreeItemsCount
				// to ConcurrentPool<T>.Get()/Return(obj)/FreeItemsCount
				if (instruction.opcode == OpCodes.Call && instruction.operand is MethodInfo methodInfo &&
					(methodInfo.Name == "Get" || methodInfo.Name == "Return" || methodInfo.Name == "get_FreeItemsCount"))
				{
					var type = methodInfo.DeclaringType;
					if (type != null && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(SimplePool<>))
					{
						var genericType = type.GenericTypeArguments[0];
						instruction.operand = AccessTools.Method(
							typeof(ConcurrentPool<>).MakeGenericType(genericType), methodInfo.Name);
					}
				}
				yield return instruction;
			}
		}
	}
}