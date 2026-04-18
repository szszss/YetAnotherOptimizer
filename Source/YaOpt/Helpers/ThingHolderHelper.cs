using System.Collections;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Verse;

namespace YaOpt.Helpers
{
	[StaticConstructorOnStartup]
	public static class ThingHolderHelper
	{
		private static readonly BitArray _thingDefHasThingHolderBloomFilter = new BitArray(ushort.MaxValue + 1);

		static ThingHolderHelper()
		{
			foreach (var thingDef in DefDatabase<ThingDef>.AllDefs)
			{
				var hash = thingDef.shortHash & 0xFFFE;
				if (typeof(IThingHolder).IsAssignableFrom(thingDef.thingClass))
				{
					_thingDefHasThingHolderBloomFilter.Set(hash, true);
				}
				if (typeof(ThingWithComps).IsAssignableFrom(thingDef.thingClass))
				{
					_thingDefHasThingHolderBloomFilter.Set(hash + 1, true);
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void MayHaveThingHolder(ThingDef thingDef,
			out bool isThingHolder, out bool isThingWithComps)
		{
			var hash = thingDef.shortHash & 0xFFFE;
			isThingHolder = _thingDefHasThingHolderBloomFilter.Get(hash);
			isThingWithComps = _thingDefHasThingHolderBloomFilter.Get(hash + 1);
		}
	}
}