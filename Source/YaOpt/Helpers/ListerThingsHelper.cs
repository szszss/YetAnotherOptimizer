using HarmonyLib;
using RimWorld;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Verse;
using static YaOpt.Helpers.ListerThingsIndexer;

namespace YaOpt.Helpers
{
	public static class ListerThingsHelper
	{
		public const int INDEX_TYPE_DEF = (int)ThingRequestGroup.ApparelSource + 1;

		/// <summary>
		/// For compatibility purposes, it records which ThingRequestGroup has index mismatch.
		/// When a removal operation is performed, the use of the indexer for this type of
		/// ThingRequestGroup will be disabled, forcing it to fall back to the vanilla path.
		/// </summary>
		private static readonly BitArray _bannedThingRequestGroup =
			new BitArray((int)ThingRequestGroup.ApparelSource + 2);

		/// <summary>
		/// Like <c>_bannedThingRequestGroup</c>, but used for <c>IHaulSource</c>.
		/// </summary>
		private static bool _banHaulSourceIndex = false;

		private static readonly AccessTools.FieldRef<ListerThings, List<Thing>[]> _fieldRefListsByGroup =
			AccessTools.FieldRefAccess<ListerThings, List<Thing>[]>(
				AccessTools.Field(typeof(ListerThings), "listsByGroup"));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void AddToThingList(List<Thing> list, Thing thing,
			ThingRecord record, ListerThingsUse use, int indexType)
		{
			list.Add(thing);
			if (use != ListerThingsUse.Global)
				return;
			var index = list.Count - 1;
			if (indexType == INDEX_TYPE_DEF)
			{
				record.DefIndex = index;
			}
			else
			{
				record.GroupIndex[indexType] = index;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static List<Thing>[] GetListsByGroup(ListerThings lister)
		{
			return _fieldRefListsByGroup(lister);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static void AddToHaulList(List<IHaulSource> list, IHaulSource haulSource,
			ThingRecord record, ListerThingsUse use)
		{
			list.Add(haulSource);
			if (use != ListerThingsUse.Global)
				return;
			var index = list.Count - 1;
			record.HaulIndex = index;
		}

		static void PrintErrorForBadIndex(Thing thing, int index, int indexType, List<Thing> list)
		{
			var listType = indexType == INDEX_TYPE_DEF
				? $"ByDef({thing.def})"
				: $"{ThingListGroupHelper.AllGroups[indexType]}";
			if (index < 0 || index >= list.Count)
			{
				YaOptMod.Warning($"Thing index is out of bound: {index} for thing {thing}. " +
							   $"List count: {list.Count}. " +
							   $"List type: {listType}. " +
							   "Fallback to the original path and the indexer has been disabled for this type.");
			}
			else
			{
				YaOptMod.Warning($"Thing does not match its index: {index} for thing {thing}. " +
								 $"The actual thing in {index} is {list[index]}. " +
								 $"List type: {listType}. " +
								 "Fallback to the original path and the indexer has been disabled for this type.");
			}
		}

		static void PrintErrorForBadIndex(IHaulSource haulSource, int index, List<IHaulSource> list)
		{
			if (index < 0 || index >= list.Count)
			{
				YaOptMod.Warning($"IHaulSource index is out of bound: {index} for IHaulSource {haulSource}. " +
								 $"List count: {list.Count}. " +
								 "Fallback to the original path and the indexer has been disabled for this type.");
			}
			else
			{
				YaOptMod.Warning($"IHaulSource does not match its index: {index} for IHaulSource {haulSource}. " +
								 $"The actual IHaulSource in {index} is {list[index]}. " +
								 "Fallback to the original path and the indexer has been disabled for this type.");
			}
		}

		internal static bool RemoveFromThingList(List<Thing> list, Thing thing,
			ListerThingsIndexer indexer, ThingRecord record, ListerThingsUse use, int indexType)
		{
			int index;

			if (use != ListerThingsUse.Global)
			{
				// For the ListerThings of a Region, we use a reverse search.
				// The existence time of things in the ListerThings of a Region is polarized.
				// They either exist for a long time or will be removed soon.
				// The latter are usually located at the end of the array.
				list.ReverseRemove(thing);
				return true;
			}

			if (_bannedThingRequestGroup.Get(indexType))
			{
				// If this ThingRequestGroup is banned, fall back to the vanilla path.
				list.ReverseRemove(thing);
				return true;
			}
			else if (indexType == INDEX_TYPE_DEF)
			{
				index = record.DefIndex;
				record.DefIndex = -1;
			}
			else
			{
				index = record.GroupIndex[indexType];
				record.GroupIndex[indexType] = -1;
			}
			var listCount = list.Count;

			if (index < 0 || index >= listCount || list[index] != thing)
			{
				// Workaround for Adaptive Storage Framework and LWM's Deep Storage
				// They will remove stored things from HasGUIOverlay list
				if (index < 0 && indexType == (int)ThingRequestGroup.HasGUIOverlay)
				{
					return true;
				}
				_bannedThingRequestGroup.Set(indexType, true);
				PrintErrorForBadIndex(thing, index, indexType, list);
				list.ReverseRemove(thing);
				return true;
			}
			if (index == listCount - 1)
			{
				list.RemoveAt(index);
				return true;
			}
			var swapThing = list[listCount - 1];
			var swapThingRecord = indexer.GetThingRecord(swapThing, use);
			list.RemoveAt(listCount - 1);
			list[index] = swapThing;
			if (indexType == INDEX_TYPE_DEF)
			{
				swapThingRecord.DefIndex = index;
			}
			else
			{
				swapThingRecord.GroupIndex[indexType] = index;
			}
			return true;
		}


		internal static bool RemoveFromHaulList(List<IHaulSource> list, IHaulSource haul,
			ListerThingsIndexer indexer, ThingRecord record, ListerThingsUse use)
		{
			int index;

			if (use != ListerThingsUse.Global)
			{
				index = list.LastIndexOf(haul);
				if (index >= 0)
					list.RemoveAt(index);
				return true;
			}
			if (_banHaulSourceIndex)
			{
				list.ReverseRemove(haul);
				return true;
			}

			index = record.HaulIndex;
			record.HaulIndex = -1;
			var listCount = list.Count;

			if (index < 0 || index >= listCount || list[index] != haul)
			{
				PrintErrorForBadIndex(haul, index, list);
				_banHaulSourceIndex = true;
				list.ReverseRemove(haul);
				return true;
			}
			if (index == listCount - 1)
			{
				list.RemoveAt(index);
				return true;
			}
			var swapThing = list[listCount - 1];
			var swapThingRecord = indexer.GetThingRecord((Thing)swapThing, use);
			list.RemoveAt(listCount - 1);
			list[index] = swapThing;
			swapThingRecord.HaulIndex = index;
			return true;
		}

		internal static void RebuildIndex(ListerThings lister, ThingRequestGroup group)
		{
			// Region ListerThings haven't indexer
			if (lister.use != ListerThingsUse.Global)
				return;

			// No need to rebuild the index for banned group
			if (_bannedThingRequestGroup.Get((int)group))
				return;

			var lists = GetListsByGroup(lister);
			var indexType = (int)group;
			var list = lists[indexType];
			if (list == null)
				return;

			var indexer = GetListerThingsIndex(lister);

			for (var i = 0; i < list.Count; i++)
			{
				var thing = list[i];
				var record = indexer.TryGetThingRecord(thing, lister.use);
				if (record != null)
				{
					record.GroupIndex[indexType] = i;
				}
			}
		}
	}
}