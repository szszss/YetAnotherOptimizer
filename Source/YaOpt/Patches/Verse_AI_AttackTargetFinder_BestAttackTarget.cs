using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse.AI;
using YaOpt.Helpers;
using YaOpt.Helpers.ThreadSafe;

namespace YaOpt.Patches
{
	/// <seealso cref="YaOptSettings.OptAttackTargetFinder"/>
	internal static class Verse_AI_AttackTargetFinder_BestAttackTarget
	{
		[ThreadStatic]
		private static Dictionary<int, bool> _validationCache;

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
				if (_validationCache == null)
					_validationCache = new Dictionary<int, bool>();
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