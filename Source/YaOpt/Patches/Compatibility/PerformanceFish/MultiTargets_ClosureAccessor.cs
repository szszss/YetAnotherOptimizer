using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Verse;

namespace YaOpt.Patches.Compatibility.PerformanceFish
{
	[HarmonyPatch]
	internal static class MultiTargets_ClosureAccessor
	{
		private static ThreadLocal<object> _delegateTL;

		private static ThreadLocal<object> _thingValidatorTL;

		private static Type _delegateInstanceType;

		private static Type _thingValidatorInstanceType;

		private static bool _initialized;

		public static object GetDelegateInstance() => _delegateTL.Value;

		public static object GetThingValidatorInstance() => _thingValidatorTL.Value;

		public static bool EnsureInitialized()
		{
			if (_initialized)
				return true;

			var prepatchesType = AccessTools.TypeByName("PerformanceFish.JobSystem.WorkGiver_DoBillPrepatches");
			if (prepatchesType == null)
				return false;

			var delegateProp = prepatchesType.GetProperty("_delegateInstance",
				BindingFlags.Public | BindingFlags.Static);
			var thingValidatorProp = prepatchesType.GetProperty("_thingValidatorInstance",
				BindingFlags.Public | BindingFlags.Static);
			if (delegateProp == null || thingValidatorProp == null)
				return false;

			var delegateSingleton = delegateProp.GetValue(null);
			var thingValidatorSingleton = thingValidatorProp.GetValue(null);
			if (delegateSingleton == null || thingValidatorSingleton == null)
				return false;

			_delegateTL = new ThreadLocal<object>(
				() => Activator.CreateInstance(delegateSingleton.GetType()));
			_thingValidatorTL = new ThreadLocal<object>(
				() => Activator.CreateInstance(thingValidatorSingleton.GetType()));
			_delegateInstanceType = delegateProp.PropertyType;
			_thingValidatorInstanceType = thingValidatorProp.PropertyType;

			_initialized = true;
			return true;
		}

		static bool Prepare()
		{
			var shouldDo = YaOptGlobal.Settings.OptParallelWorkGiver.Enabled && YaOptGlobal.HasMod("bs.performance");
			if (shouldDo)
			{
				EnsureInitialized();
			}
			return shouldDo;
		}

		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(
				AccessTools.TypeByName("PerformanceFish.JobSystem.WorkGiver_DoBillOptimization/TryFindBestIngredientsHelper_Patch"),
				"Postfix");
			foreach (var nestedType in typeof(WorkGiver_DoBill).GetNestedTypes(
						 BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic))
			{
				var method = AccessTools.FirstMethod(nestedType, methodInfo =>
				{
					var param = methodInfo.GetParameters();
					return param?.Length == 1 && param[0].ParameterType == typeof(Region) &&
						   (methodInfo.Name.Contains("<TryFindBestIngredientsHelper>b__4"));
				});
				if (method != null)
				{
					yield return method;
					yield break;
				}
			}
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var getDelegateMethod = AccessTools.Method(
				typeof(MultiTargets_ClosureAccessor),
				nameof(GetDelegateInstance));
			var getThingValidatorMethod = AccessTools.Method(
				typeof(MultiTargets_ClosureAccessor),
				nameof(GetThingValidatorInstance));

			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Call && instruction.operand is MethodInfo method)
				{
					if (method.Name == "get__delegateInstance")
					{
						yield return new CodeInstruction(OpCodes.Call, getDelegateMethod);
						yield return new CodeInstruction(OpCodes.Castclass, _delegateInstanceType);
						continue;
					}
					if (method.Name == "get__thingValidatorInstance")
					{
						yield return new CodeInstruction(OpCodes.Call, getThingValidatorMethod);
						yield return new CodeInstruction(OpCodes.Castclass, _thingValidatorInstanceType);
						continue;
					}
				}
				yield return instruction;
			}
		}
	}
}