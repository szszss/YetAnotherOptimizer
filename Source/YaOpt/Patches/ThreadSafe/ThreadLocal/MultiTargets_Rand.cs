using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;
using Verse;

namespace YaOpt.Patches.ThreadSafe.ThreadLocal
{
	[HarmonyPatch]
	internal static class MultiTargets_Rand
	{
		[ThreadStatic]
		private static uint _seed;

		[ThreadStatic]
		private static uint _iterations;

		[ThreadStatic]
		private static bool _inited;

		private static ThreadLocal<Stack<ulong>> _threadLocalStateStack =
			new ThreadLocal<Stack<ulong>>(() => new Stack<ulong>());

		private static ThreadLocal<List<int>> _threadLocalTmpRange =
			new ThreadLocal<List<int>>(() => new List<int>());

		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.PropertySetter(typeof(Rand), nameof(Rand.Seed));
			yield return AccessTools.PropertySetter(typeof(Rand), "StateCompressed");
			yield return AccessTools.PropertyGetter(typeof(Rand), "StateCompressed");
			yield return AccessTools.PropertyGetter(typeof(Rand), nameof(Rand.Int));
			yield return AccessTools.PropertyGetter(typeof(Rand), nameof(Rand.Value));
			yield return AccessTools.Method(typeof(Rand), nameof(Rand.TryRangeInclusiveWhere));
			yield return AccessTools.Method(typeof(Rand), nameof(Rand.PushState));
			yield return AccessTools.Method(typeof(Rand), nameof(Rand.PopState));
			yield return AccessTools.Method(typeof(Rand), nameof(Rand.EnsureStateStackEmpty));
		}

		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void Init()
		{
			if (!_inited)
				InitDo();
		}

		private static void InitDo()
		{
			_inited = true;
			_seed = (uint)AccessTools.Field(typeof(Rand), "seed").GetValue(null);
			_iterations = YaOptGlobal.IsInMainThread ? 0 : (uint)Thread.CurrentThread.ManagedThreadId;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var list = instructions.ToArray();
			var usedStateStack = false;
			var usedTmpRange = false;
			LocalBuilder localStateStack = null;
			LocalBuilder localTmpRange = null;
			foreach (var instruction in list)
			{
				if (instruction.operand is FieldInfo fieldInfo)
				{
					if (fieldInfo.Name == "stateStack")
					{
						usedStateStack = true;
					}
					else if (fieldInfo.Name == "tmpRange")
					{
						usedTmpRange = true;
					}
				}
			}

			yield return CodeInstruction.Call(typeof(MultiTargets_Rand), nameof(Init));
			if (usedStateStack)
			{
				localStateStack = generator.DeclareLocal(typeof(Stack<ulong>));
				yield return CodeInstruction.LoadField(
					typeof(MultiTargets_Rand), nameof(_threadLocalStateStack));
				yield return new CodeInstruction(OpCodes.Call,
					AccessTools.PropertyGetter(typeof(ThreadLocal<Stack<ulong>>), "Value"));
				yield return CodeInstruction.StoreLocal(localStateStack.LocalIndex);
			}
			if (usedTmpRange)
			{
				localTmpRange = generator.DeclareLocal(typeof(List<int>));
				yield return CodeInstruction.LoadField(
					typeof(MultiTargets_Rand), nameof(_threadLocalTmpRange));
				yield return new CodeInstruction(OpCodes.Call,
					AccessTools.PropertyGetter(typeof(ThreadLocal<List<int>>), "Value"));
				yield return CodeInstruction.StoreLocal(localTmpRange.LocalIndex);
			}
			foreach (var instruction in list)
			{
				if (instruction.operand is FieldInfo fieldInfo)
				{
					if (fieldInfo.Name == "seed")
					{
						instruction.operand = AccessTools.Field(typeof(MultiTargets_Rand), nameof(_seed));
					}
					else if (fieldInfo.Name == "iterations")
					{
						instruction.operand = AccessTools.Field(typeof(MultiTargets_Rand), nameof(_iterations));
					}
					else if (usedStateStack && fieldInfo.Name == "stateStack")
					{
						yield return CodeInstruction.LoadLocal(localStateStack.LocalIndex)
							.WithLabels(instruction.labels);
						continue;
					}
					else if (usedTmpRange && fieldInfo.Name == "tmpRange")
					{
						yield return CodeInstruction.LoadLocal(localTmpRange.LocalIndex)
							.WithLabels(instruction.labels);
						continue;
					}
				}
				yield return instruction;
			}
		}
	}
}