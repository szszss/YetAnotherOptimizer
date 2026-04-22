using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using Verse.AI;
using YaOpt.Helpers;
using YaOpt.Helpers.ThreadSafe;

namespace YaOpt.Patches
{
	/// <seealso cref="YaOptSettings.OptAttackTargetFinder"/>
	internal static class Verse_AI_AttackTargetFinder_BestAttackTarget
	{
		private static readonly Dictionary<int, bool> _validationCache = new Dictionary<int, bool>();

		[HarmonyPatch(typeof(AttackTargetFinder))]
		[HarmonyPatch(nameof(AttackTargetFinder.BestAttackTarget))]
		private static class MainPart
		{
			static bool Prepare()
			{
				return YaOptGlobal.Settings.OptAttackTargetFinder.Enabled &&
				       !YaOptGlobal.HasMod("Vortex.Kingfisher");
			}

			static void Prefix()
			{
				_validationCache.Clear();
			}

			static void Postfix()
			{
				_validationCache.Clear();
			}
		}

		[HarmonyPatch]
		private static class ClosurePart
		{
			// JobPredictor.PredictDoConstantJob will use this concurrently.
			private static GreedySpinLock _spinLock = new GreedySpinLock();

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
				return YaOptGlobal.Settings.OptAttackTargetFinder.Enabled &&
				       !YaOptGlobal.HasMod("Vortex.Kingfisher");
			}

			static bool Prefix(IAttackTarget __0, ref bool __result)
			{
				_spinLock.Enter();
				try
				{
					return !_validationCache.TryGetValue(__0.Thing.thingIDNumber, out __result);
				}
				finally
				{
					_spinLock.Exit();
				}
			}

			static void Postfix(IAttackTarget __0, ref bool __result, bool __runOriginal)
			{
				if (__runOriginal)
				{
					_spinLock.Enter();
					try
					{
						_validationCache[__0.Thing.thingIDNumber] = __result;
					}
					finally
					{
						_spinLock.Exit();
					}
				}
			}
		}
	}
}