using System.Threading;
using Verse;

namespace YaOpt.Helpers
{
	internal static class ConcurrentUniqueIDHelper
	{
		public static int GetNextIdThreadSafely(ref int id, bool uidManagerLoaded = true)
		{
			if (Scribe.mode == LoadSaveMode.LoadingVars && !uidManagerLoaded)
			{
				Log.Warning("Getting next unique ID during LoadingVars before UniqueIDsManager was loaded. Assigning a random value.");
				return Rand.Int;
			}
			if (Scribe.mode == LoadSaveMode.Saving)
			{
				Log.Warning("Getting next unique ID during saving This may cause bugs.");
			}
			while (true)
			{
				var original = id;
				var next = (original == int.MaxValue) ? 0 : original + 1;
				var result = Interlocked.CompareExchange(ref id, next, original);
				if (result == original)
				{
					if (original == int.MaxValue)
						Log.Warning("Next ID is at max value. Resetting to 0. This may cause bugs.");
					return next;
				}
			}
		}
	}
}