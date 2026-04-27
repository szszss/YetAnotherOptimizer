using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Verse;
using YaOpt.Helpers;
using YaOpt.Patches.Prepatch;

namespace YaOpt.Patches
{
	/// <seealso cref="YaOptSettings.OptThingGetComp"/>
	[HarmonyPatch]
	internal static class MultiTargets_ThingGetComp
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(
				typeof(GenHostility),
				nameof(GenHostility.HostileTo), new[] { typeof(Thing), typeof(Faction) });
			yield return AccessTools.Method(
				typeof(GenHostility),
				nameof(GenHostility.HostileTo), new[] { typeof(Thing), typeof(Thing) });
			yield return AccessTools.PropertyGetter(
				typeof(Building_Grave),
				nameof(Building_Grave.CompAssignableToPawn));
			yield return AccessTools.Method(
				typeof(ThingWithComps),
				nameof(ThingWithComps.Notify_Explosion));
			yield return AccessTools.Method(
				typeof(StatWorker),
				nameof(StatWorker.StatOffsetFromGear));
			yield return AccessTools.Method(
				typeof(CompRefuelable),
				nameof(CompRefuelable.CompTick));
			yield return AccessTools.Method(
				typeof(Pawn),
				nameof(Pawn.ThreatDisabled));
		}

		static bool Prepare()
		{
			return Verse_ThingWithComps_GetComp.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				if (instruction.Calls("TryGetComp") && instruction.operand is MethodInfo method1 &&
					method1.IsGenericMethod && method1.GetParameters().Length == 1)
				{
					var type = method1.GetGenericArguments()[0];
					if (type == typeof(CompCauseGameCondition))
					{
						yield return CodeInstruction.Call(typeof(MultiTargets_ThingGetComp),
							nameof(TryGetCompCauseGameCondition));
						continue;
					}
					if (type == typeof(CompAssignableToPawn_Grave))
					{
						yield return CodeInstruction.Call(typeof(MultiTargets_ThingGetComp),
							nameof(TryGetCompAssignableToPawnGrave));
						continue;
					}
					if (type == typeof(CompWakeUpDormant))
					{
						yield return CodeInstruction.Call(typeof(MultiTargets_ThingGetComp),
							nameof(TryGetCompWakeUpDormant));
						continue;
					}
					if (type == typeof(CompBladelinkWeapon))
					{
						yield return CodeInstruction.Call(typeof(MultiTargets_ThingGetComp),
							nameof(TryGetCompBladelinkWeapon));
						continue;
					}
					if (type == typeof(CompPowerTrader))
					{
						yield return CodeInstruction.Call(typeof(MultiTargets_ThingGetComp),
							nameof(TryGetCompPowerTrader));
						continue;
					}
				}
				else if (instruction.Calls("GetComp") && instruction.operand is MethodInfo method2 &&
						 method2.IsGenericMethod)
				{
					var type = method2.GetGenericArguments()[0];
					if (type == typeof(CompCauseGameCondition))
					{
						yield return CodeInstruction.Call(typeof(MultiTargets_ThingGetComp),
							nameof(GetCompCauseGameCondition));
						continue;
					}
					if (type == typeof(CompAssignableToPawn_Grave))
					{
						yield return CodeInstruction.Call(typeof(MultiTargets_ThingGetComp),
							nameof(GetCompAssignableToPawnGrave));
						continue;
					}
					if (type == typeof(CompWakeUpDormant))
					{
						yield return CodeInstruction.Call(typeof(MultiTargets_ThingGetComp),
							nameof(GetCompWakeUpDormant));
						continue;
					}
					if (type == typeof(CompBladelinkWeapon))
					{
						yield return CodeInstruction.Call(typeof(MultiTargets_ThingGetComp),
							nameof(GetCompBladelinkWeapon));
						continue;
					}
					if (type == typeof(CompPowerTrader))
					{
						yield return CodeInstruction.Call(typeof(MultiTargets_ThingGetComp),
							nameof(GetCompPowerTrader));
						continue;
					}
				}
				if (instruction.Calls("TryGetComp") && instruction.operand is MethodInfo method3 &&
					method3.IsGenericMethod && method3.GetParameters().Length == 2)
				{
					var type = method3.GetGenericArguments()[0];
					if (type == typeof(CompActivity))
					{
						yield return CodeInstruction.Call(typeof(MultiTargets_ThingGetComp),
							nameof(TryGetCompActivity));
						continue;
					}
				}
				yield return instruction;
			}
		}

		#region CompCauseGameCondition
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static CompCauseGameCondition TryGetCompCauseGameCondition(Thing thing)
		{
			if (thing is ThingWithComps thingWithComps)
			{
				return GetCompCauseGameCondition(thingWithComps);
			}
			return null;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static CompCauseGameCondition GetCompCauseGameCondition(ThingWithComps thingWithComps)
		{
			return thingWithComps.YaOptStruct().CauseGameCondition;
		}
		#endregion

		#region CompAssignableToPawn_Grave
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static CompAssignableToPawn_Grave TryGetCompAssignableToPawnGrave(Thing thing)
		{
			if (thing is ThingWithComps thingWithComps)
			{
				return GetCompAssignableToPawnGrave(thingWithComps);
			}
			return null;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static CompAssignableToPawn_Grave GetCompAssignableToPawnGrave(ThingWithComps thingWithComps)
		{
			return thingWithComps.YaOptStruct().AssignableToPawnGrave;
		}
		#endregion

		#region CompWakeUpDormant
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static CompWakeUpDormant TryGetCompWakeUpDormant(Thing thing)
		{
			if (thing is ThingWithComps thingWithComps)
			{
				return GetCompWakeUpDormant(thingWithComps);
			}
			return null;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static CompWakeUpDormant GetCompWakeUpDormant(ThingWithComps thingWithComps)
		{
			return thingWithComps.YaOptStruct().WakeUpDormant;
		}
		#endregion

		#region CompBladelinkWeapon
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static CompBladelinkWeapon TryGetCompBladelinkWeapon(Thing thing)
		{
			if (thing is ThingWithComps thingWithComps)
			{
				return GetCompBladelinkWeapon(thingWithComps);
			}
			return null;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static CompBladelinkWeapon GetCompBladelinkWeapon(ThingWithComps thingWithComps)
		{
			return thingWithComps.YaOptStruct().BladelinkWeapon;
		}
		#endregion

		#region CompPowerTrader
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static CompPowerTrader TryGetCompPowerTrader(Thing thing)
		{
			if (thing is ThingWithComps thingWithComps)
			{
				return GetCompPowerTrader(thingWithComps);
			}
			return null;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static CompPowerTrader GetCompPowerTrader(ThingWithComps thingWithComps)
		{
			return thingWithComps.YaOptStruct().PowerTrader;
		}
		#endregion

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool TryGetCompActivity(Pawn pawn, out CompActivity comp)
		{
			comp = pawn.activity;
			return comp != null;
		}
	}
}