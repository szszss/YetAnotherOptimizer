using HarmonyLib;
using RimWorld;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Verse;

namespace YaOpt.Patches.ThreadSafe.Delayed
{
	[HarmonyPatch(typeof(SteadyEnvironmentEffects))]
	[HarmonyPatch(nameof(SteadyEnvironmentEffects.SteadyEnvironmentEffectsTick))]
	internal static class RimWorld_SteadyEnvironmentEffects_SteadyEnvironmentEffectsTick
	{
		private static readonly ConcurrentQueue<(Map, int, int)> _delayedCellSteadyEffects =
			new ConcurrentQueue<(Map, int, int)>();

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelPostMapTick.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				yield return instruction;

				// Insert
				// AddDelayedCellSteadyEffects(this.map, this.cycleIndex, num);
				// After
				// int num = Mathf.CeilToInt((float)this.map.Area * 0.0006f);
				if (instruction.opcode == OpCodes.Stloc_0)
				{
					yield return CodeInstruction.LoadArgument(0);
					yield return CodeInstruction.LoadField(typeof(SteadyEnvironmentEffects), "map");
					yield return CodeInstruction.LoadArgument(0);
					yield return CodeInstruction.LoadField(typeof(SteadyEnvironmentEffects), "cycleIndex");
					yield return CodeInstruction.LoadLocal(0);
					yield return CodeInstruction.Call(
						typeof(RimWorld_SteadyEnvironmentEffects_SteadyEnvironmentEffectsTick),
						nameof(AddDelayedCellSteadyEffects));
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void AddDelayedCellSteadyEffects(Map map, int cycleIndex, int count)
		{
			_delayedCellSteadyEffects.AddItem((map, cycleIndex, count));
		}

		public static void Playback()
		{
			while (_delayedCellSteadyEffects.TryDequeue(out var result))
			{
				var map = result.Item1;
				var cycleIndex = result.Item2;
				var count = result.Item3;
				var area = map.Area;
				for (var i = 0; i < count; i++)
				{
					if (cycleIndex >= area)
					{
						cycleIndex = 0;
					}
					var intVec = map.cellsInRandomOrder.Get(cycleIndex);
					map.gameConditionManager.DoSteadyEffects(intVec, map);
					GasUtility.DoSteadyEffects(intVec, map);
				}
			}
		}

		public static void Clear()
		{
			_delayedCellSteadyEffects.Clear();
		}
	}
}