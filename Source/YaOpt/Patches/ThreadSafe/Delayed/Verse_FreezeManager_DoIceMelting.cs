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
	[HarmonyPatch("DoIceMelting")]
	internal static class Verse_FreezeManager_DoIceMelting
	{
		private static readonly ConcurrentQueue<(TerrainGrid, IntVec3)> _delayedRemoveTempTerrain =
			new ConcurrentQueue<(TerrainGrid, IntVec3)>();

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelPostMapTick.Enabled;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static void Delay(TerrainGrid terrainGrid, IntVec3 vec3, bool doLeavings, bool preventDestroyEffects)
		{
			if (ParallelMapTickManager.ShouldCheckThread && JobsUtility.IsExecutingJob)
			{
				_delayedRemoveTempTerrain.Enqueue((terrainGrid, vec3));
			}
			else
			{
				terrainGrid.RemoveTempTerrain(vec3, doLeavings, preventDestroyEffects);
			}
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				if (instruction.Calls("RemoveTempTerrain"))
				{
					yield return CodeInstruction.Call(typeof(Verse_FreezeManager_DoIceMelting), nameof(Delay));
					continue;
				}
				yield return instruction;
			}
		}

		public static void Playback()
		{
			if (!_delayedRemoveTempTerrain.IsEmpty)
			{
				while (_delayedRemoveTempTerrain.TryDequeue(out var result))
				{
					result.Item1.RemoveTempTerrain(result.Item2, false, false);
				}
			}
		}

		public static void Clear()
		{
			_delayedRemoveTempTerrain.Clear();
		}
	}
}