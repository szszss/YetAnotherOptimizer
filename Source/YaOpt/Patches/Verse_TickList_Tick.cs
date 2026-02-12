using HarmonyLib;
using System;
using System.Collections.Generic;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <summary>
	/// <seealso cref="YaOptSettings.OptParallelPawnTick"/>
	/// </summary>
	[HarmonyPatch(typeof(TickList))]
	[HarmonyPatch(nameof(TickList.Tick))]
	internal static class Verse_TickList_Tick
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelPawnTick.Enabled;
		}

		// Register/Unregister pawns to ParallelTickerManager
		static void Prefix(TickerType ___tickType,
			List<Thing> ___thingsToRegister, List<Thing> ___thingsToDeregister)
		{
			if (___tickType == TickerType.Normal)
			{
				ParallelTickManager.AddThings(___thingsToRegister);
				ParallelTickManager.RemoveThings(___thingsToDeregister);
			}
		}

		// Tick all pawns
		static void Postfix(TickerType ___tickType, List<List<Thing>> ___thingLists)
		{
			if (___tickType == TickerType.Normal)
			{
				if (___thingLists.Count > 1)
				{
					ParallelTickManager.ParellellyTickPawns();
					foreach (var thing in ___thingLists[1])
					{
						if (thing.Destroyed)
							continue;
						try
						{
							thing.DoTick();
						}
						catch (Exception ex)
						{
							string text = (thing.Spawned ? $" (at {thing.Position})" : "");
							if (Prefs.DevMode)
							{
								Log.Error($"Exception ticking {thing.ToStringSafe()}{text}: {ex}");
							}
							else
							{
								Log.ErrorOnce(
									$"Exception ticking {thing.ToStringSafe()}{text}. " +
									$"Suppressing further errors. Exception: {ex}", thing.thingIDNumber ^ 576876901);
							}
						}
					}
				}
				else
				{
					// TODO: error
				}
			}
		}
	}
}