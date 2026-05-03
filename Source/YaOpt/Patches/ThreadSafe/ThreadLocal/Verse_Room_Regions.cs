using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches.ThreadSafe.ThreadLocal
{
	[HarmonyPatch(typeof(Room))]
	[HarmonyPatch(nameof(Room.Regions), MethodType.Getter)]
	internal static class Verse_Room_Regions
	{
		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe;
		}

		// Old implementation: It fails when a thread uses two Room.Regions simultaneously.
		/*
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			return ThreadLocalHelper.ThreadLocalTranspiler(instructions, generator, "tmpRegions");
		}
		*/

		// It's still not truly robust; it fails when other threads attempt to modify Room.Regions,
		// but it's better than the old implementation.
		static bool Prefix(List<Region> ___tmpRegions, List<District> ___districts, ref List<Region> __result)
		{
			__result = ___tmpRegions;
			List<Region> tmpList = null;
			try
			{
				tmpList = ConcurrentPool<List<Region>>.Get();
				tmpList.Clear();
				for (int i = 0; i < ___districts.Count; i++)
				{
					var regions = ___districts[i].Regions;
					for (var j = 0; j < regions.Count; j++)
					{
						tmpList.Add(regions[j]);
					}
				}
				lock (___tmpRegions)
				{
					if (!___tmpRegions.SequenceEqual(tmpList))
					{
						___tmpRegions.Clear();
						___tmpRegions.AddRange(tmpList);
					}
				}
			}
			finally
			{
				if (tmpList != null)
				{
					tmpList.Clear();
					ConcurrentPool<List<Region>>.Return(tmpList);
				}
			}
			return false;
		}
	}
}