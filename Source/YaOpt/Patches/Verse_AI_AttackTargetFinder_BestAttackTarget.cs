using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using Verse.AI;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <seealso cref="YaOptSettings.OptAttackTargetFinder"/>
	internal static class Verse_AI_AttackTargetFinder_BestAttackTarget
	{
		private static readonly Dictionary<int, bool> _validationCache = new Dictionary<int, bool>();

		private readonly struct Comparer : IComparer<IAttackTarget>
		{
			private readonly IntVec3 _shooterPosition;

			public Comparer(IntVec3 shooterPosition)
			{
				_shooterPosition = shooterPosition;
			}

			public int Compare(IAttackTarget a, IAttackTarget b)
			{
				if (a == null || b == null)
					return 0;
				var lenA = (a.Thing.Position - _shooterPosition).LengthManhattan;
				var lenB = (b.Thing.Position - _shooterPosition).LengthManhattan;
				return lenA.CompareTo(lenB);
			}
		}

		private static int SortTargetList(List<IAttackTarget> targets,
			Predicate<IAttackTarget> predicate, IAttackTargetSearcher searcher)
		{
			var num = targets.RemoveAll(predicate);
			var comparer = new Comparer(searcher.Thing.Position);
			targets.Sort(comparer);
			return num;
		}

		[HarmonyPatch(typeof(AttackTargetFinder))]
		[HarmonyPatch(nameof(AttackTargetFinder.BestAttackTarget))]
		private static class MainPart
		{
			static bool Prepare()
			{
				return YaOptGlobal.Settings.OptAttackTargetFinder.Enabled;
			}

			static void Prefix()
			{
				_validationCache.Clear();
			}

			/*static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
			{
				var firstRemoveAll = true;
				foreach (var instruction in instructions)
				{
					if (instruction.Calls("RemoveAll") && firstRemoveAll)
					{
						firstRemoveAll = false;
						yield return CodeInstruction.LoadArgument(0);
						yield return CodeInstruction.Call(
							typeof(Verse_AI_AttackTargetFinder_BestAttackTarget),
							nameof(SortTargetList));
						continue;
					}
					yield return instruction;
				}
			}*/

			static void Postfix()
			{
				_validationCache.Clear();
			}
		}

		[HarmonyPatch]
		private static class ClosurePart
		{
			static MethodBase TargetMethod()
			{
				MethodInfo method = null;
				foreach (var nestedType in typeof(AttackTargetFinder).GetNestedTypes(
							 BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic))
				{
					if (nestedType.GetField("searcher",
							BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public) == null)
						continue;

					method = nestedType.GetMethod("<BestAttackTarget>b__1",
						BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					if (method != null)
					{
						YaOptMod.Debug("Verse_AI_AttackTargetFinder_BestAttackTarget " +
									   $"found a method from BestAttackTarget: {method.FullName()}");
						break;
					}
				}
				if (method == null)
				{
					throw new MissingMethodException("Cannot find closure method for " +
													 "AttackTargetFinder.BestAttackTarget");
				}
				return method;
			}

			static bool Prepare()
			{
				return YaOptGlobal.Settings.OptAttackTargetFinder.Enabled;
			}

			static bool Prefix(IAttackTarget __0, ref bool __result)
			{
				return !_validationCache.TryGetValue(__0.Thing.thingIDNumber, out __result);
			}

			static void Postfix(IAttackTarget __0, ref bool __result, bool __runOriginal)
			{
				if (__runOriginal)
				{
					_validationCache[__0.Thing.thingIDNumber] = __result;
				}
			}
		}
	}
}