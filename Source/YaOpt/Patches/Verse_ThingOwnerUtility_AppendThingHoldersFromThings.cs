using HarmonyLib;
using System.Collections.Generic;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <seealso cref="YaOptSettings.OptAppendThingHolders"/>
	[HarmonyPatch(typeof(ThingOwnerUtility))]
	[HarmonyPatch(nameof(ThingOwnerUtility.AppendThingHoldersFromThings))]
	internal static class Verse_ThingOwnerUtility_AppendThingHoldersFromThings
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptAppendThingHolders.Enabled;
		}

		static bool Prefix(List<IThingHolder> outThingsHolders, IList<Thing> container)
		{
			if (container == null)
			{
				return false;
			}
			var i = 0;
			var count = container.Count;
			while (i < count)
			{
				var thing = container[i];
				ThingHolderHelper.MayHaveThingHolder(thing.def,
					out var isThingHolder, out var isThingWithComps);
				if (isThingHolder && thing is IThingHolder thingHolder)
				{
					outThingsHolders.Add(thingHolder);
				}
				if (isThingWithComps && thing is ThingWithComps thingWithComps)
				{
					GetCompHelper.RetrieveThingHolderComps(thingWithComps, outThingsHolders);
				}
				i++;
			}
			return false;
		}
	}
}