using HarmonyLib;
using System.Collections.Generic;
using Verse;

namespace YaOpt.Patches
{
	/// <summary>
	/// <seealso cref="YaOptSettings.OptParallelPawnTick"/>
	/// </summary>
	[HarmonyPatch(typeof(TickList))]
	[HarmonyPatch(MethodType.Constructor, typeof(TickerType))]
	internal static class Verse_TickList_Constructor
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelPawnTick.Enabled;
		}

		static void Postfix(TickerType ___tickType, List<List<Thing>> ___thingLists)
		{
			if (___tickType == TickerType.Normal)
			{
				___thingLists.Add(new List<Thing>());
			}
		}
	}
}