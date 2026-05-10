using System;
using System.Collections.Generic;
using Verse;

namespace YaOpt.Helpers
{
	internal class ListerThingsIndexer
	{
		private const int GROUP_COUNT = (int)ThingRequestGroup.ApparelSource + 1;

		private static readonly Dictionary<ListerThings, ListerThingsIndexer> _indexers =
			new Dictionary<ListerThings, ListerThingsIndexer>();

		private static readonly ListerThingsIndexer _dummyListerThingsIndex = new ListerThingsIndexer();

		private static readonly ThingRecord _dummyRecord = new ThingRecord();

		private readonly Dictionary<Thing, ThingRecord> _records = new Dictionary<Thing, ThingRecord>();

		internal sealed class ThingRecord
		{
			public int DefIndex = -1;
			public int HaulIndex = -1;
			public int[] GroupIndex = new int[GROUP_COUNT];

			public ThingRecord()
			{
				Array.Fill(GroupIndex, -1);
			}
		}

		static ListerThingsIndexer()
		{
			UpdateCallbackHelper.RegisterClearCacheCallback(ClearCache);
		}

		private static void ClearCache()
		{
			_indexers.Clear();
		}

		public static void Create(ListerThings lister)
		{
			if (lister.use != ListerThingsUse.Global)
				return;
			_indexers[lister] = new ListerThingsIndexer();
		}

		public static void Destroy(ListerThings lister)
		{
			if (lister.use != ListerThingsUse.Global)
				return;
			_indexers.Remove(lister);
		}

		public static ListerThingsIndexer GetListerThingsIndex(ListerThings lister)
		{
			if (lister.use != ListerThingsUse.Global)
				return _dummyListerThingsIndex;
			if (!_indexers.TryGetValue(lister, out var indexer))
			{
				YaOptMod.Error("Cannot find record for ListerThings.");
				return _dummyListerThingsIndex;
			}
			return indexer;
		}

		public ThingRecord Add(Thing thing, ListerThingsUse use)
		{
			if (use != ListerThingsUse.Global)
				return _dummyRecord;
			if (_records.TryGetValue(thing, out var value))
			{
				YaOptMod.Error("Attempting to create record for a thing multiple times in ListerThingsIndexer.");
				return value;
			}
			value = new ThingRecord();
			_records[thing] = value;
			return value;
		}

		public void Remove(Thing thing, ListerThingsUse use)
		{
			if (use != ListerThingsUse.Global)
				return;
			if (!_records.Remove(thing))
			{
				YaOptMod.Error("Attempting to remove record for a thing multiple times in ListerThingsIndexer.");
			}
		}

		public void Clear()
		{
			_records.Clear();
		}

		public ThingRecord GetThingRecord(Thing thing, ListerThingsUse use)
		{
			if (use != ListerThingsUse.Global)
				return _dummyRecord;
			if (!_records.TryGetValue(thing, out var value))
			{
				YaOptMod.Error($"ListerThingsIndex cannot find the entry of Thing {thing}.");
				return new ThingRecord();
			}
			return value;
		}

		public ThingRecord TryGetThingRecord(Thing thing, ListerThingsUse use)
		{
			if (use != ListerThingsUse.Global)
				return null;
			return _records.GetValueOrDefault(thing);
		}
	}
}