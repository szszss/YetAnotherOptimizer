using HarmonyLib;
using MVCF.Utilities;
using MVCF.VerbComps;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using Verse.AI;
using YaOpt.Helpers;

namespace YaOpt.OtherMod.VanillaExpandedFramework.Patches.ThreadSafe
{
	[HarmonyPatch]
	[HarmonyAfter("OskarPotocki.VEF")]
	internal static class MultiTargets_SearchVerb
	{
		[ThreadStatic]
		public static Verb SearchVerb;

		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(
				typeof(TargetFinder),
				nameof(TargetFinder.BestAttackTarget),
				new[]{ typeof(IAttackTargetSearcher),
					typeof(Verb).MakeByRefType(),
					typeof(TargetScanFlags),
					typeof(Predicate<Thing>),
					typeof(float),
					typeof(float),
					typeof(IntVec3),
					typeof(float),
					typeof(bool),
					typeof(bool),
					typeof(bool),
					typeof(bool),
					typeof(bool),
					typeof(bool)
				});
			yield return AccessTools.Method(
				typeof(VerbComp_Turret),
				"TryFindNewTarget");
		}

		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				if (instruction.Calls("BestAttackTarget") && instruction.operand is MethodInfo method &&
					method.GetParameters().Length == 12)
				{
					instruction.operand = AccessTools.Method(
						typeof(MultiTargets_SearchVerb),
						nameof(BestAttackTarget));
				}
				yield return instruction;
			}
		}

		private static IAttackTarget BestAttackTarget(IAttackTargetSearcher searcher, Verb verb, TargetScanFlags flags,
			Predicate<Thing> validator, float minDist, float maxDist, IntVec3 locus, float maxTravelRadiusFromLocus,
			bool canBashDoors, bool canTakeTargetsCloserThanEffectiveMinRange, bool canBashFences, bool onlyRanged)
		{
			SearchVerb = verb;
			if (verb.IsIncendiary_Ranged())
			{
				flags |= TargetScanFlags.NeedNonBurning;
			}
			var attackTarget = AttackTargetFinder.BestAttackTarget(searcher, flags, validator,
				minDist, maxDist, locus, maxTravelRadiusFromLocus, canBashDoors,
				canTakeTargetsCloserThanEffectiveMinRange, canBashFences, onlyRanged);
			SearchVerb = null;
			return attackTarget;
		}
	}
}