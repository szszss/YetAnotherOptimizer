using HarmonyLib;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Jobs.LowLevel.Unsafe;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches.ThreadSafe.Delayed
{
	[HarmonyPatch(typeof(FreezeManager))]
	[HarmonyPatch("DoWaterFreezing")]
	internal static class Verse_FreezeManager_DoWaterFreezing
	{
		private static readonly ConcurrentQueue<(TerrainGrid, IntVec3, TerrainDef)> _delayedSetTempTerrain =
			new ConcurrentQueue<(TerrainGrid, IntVec3, TerrainDef)>();

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelPostMapTick.Enabled;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static void Delay(TerrainGrid terrainGrid, IntVec3 vec3, TerrainDef newTerr)
		{
			if (ParallelMapTickManager.ShouldCheckThread && JobsUtility.IsExecutingJob)
			{
				_delayedSetTempTerrain.Enqueue((terrainGrid, vec3, newTerr));
			}
			else
			{
				terrainGrid.SetTempTerrain(vec3, newTerr);
			}
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				if (instruction.Calls("SetTempTerrain"))
				{
					yield return CodeInstruction.Call(typeof(Verse_FreezeManager_DoWaterFreezing), nameof(Delay));
					continue;
				}
				yield return instruction;
			}
		}

		public static void Playback()
		{
			if (!_delayedSetTempTerrain.IsEmpty)
			{
				while (_delayedSetTempTerrain.TryDequeue(out var result))
				{
					result.Item1.SetTempTerrain(result.Item2, result.Item3);
				}
			}
		}

		public static void Clear()
		{
			_delayedSetTempTerrain.Clear();
		}
	}
}