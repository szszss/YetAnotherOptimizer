using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Verse;
using YaOpt.Patches.Prepatch;

namespace YaOpt.Helpers
{
	public static class GetCompHelper
	{
		public const int VERSION_MAGICNUMBER_HAS_THINGHOLDER = 0x7F000000;

		public const int VERSION_MAGICNUMBER_NO_THINGHOLDER = 0x7E000000;

		public const int VERSION_MAGICNUMBER_MASK = 0x7F000000;

		private static readonly Type _thingCompType = typeof(ThingComp);

		private static readonly AccessTools.FieldRef<List<ThingComp>, int> _listVersionFieldRef =
			AccessTools.FieldRefAccess<int>(typeof(List<ThingComp>), "_version");

		private static readonly AccessTools.FieldRef<Dictionary<Type, ThingComp[]>, int> _dictVersionFieldRef =
			AccessTools.FieldRefAccess<int>(typeof(Dictionary<Type, ThingComp[]>), "_version");

		private static readonly AccessTools.FieldRef<ThingWithComps, Dictionary<Type, ThingComp[]>> _thingCompsByTypeFieldRef =
			AccessTools.FieldRefAccess<Dictionary<Type, ThingComp[]>>(typeof(ThingWithComps), "compsByType");

		private static readonly Dictionary<Type, List<ThingComp>> _tmpCompsByType =
			new Dictionary<Type, List<ThingComp>>();

		//private static HashSet<IntPtr> usedMrgctx = new HashSet<IntPtr>();

		public static Dictionary<Type, ThingComp[]> CreateCompsByType(ThingWithComps thing, List<ThingComp> comps)
		{
			var compsByType = new Dictionary<Type, ThingComp[]>();
			RecreateCompsByType(thing, compsByType, comps);
			return compsByType;
		}

		public static void RecreateCompsByType(ThingWithComps thing,
			Dictionary<Type, ThingComp[]> compsByType, List<ThingComp> comps)
		{
			lock (_tmpCompsByType)
			{
				var objType = typeof(object);
				var hasThingHolder = false;
				var bloomFilter = new BloomFilter();
				_tmpCompsByType.Clear();
				foreach (var comp in comps)
				{
					var type = comp.GetType();
					if (!_tmpCompsByType.TryGetValue(type, out var list))
					{
						list = SimplePool<List<ThingComp>>.Get();
						_tmpCompsByType[type] = list;
					}
					list.Add(comp);
					bloomFilter.Set(type);
				}
				var interfaceThingHolder = typeof(IThingHolder);
				// Cache IThingHolder
				foreach (var comp in comps)
				{
					if (comp is IThingHolder)
					{
						hasThingHolder = true;
						if (!_tmpCompsByType.TryGetValue(interfaceThingHolder, out var list))
						{
							list = SimplePool<List<ThingComp>>.Get();
							_tmpCompsByType[interfaceThingHolder] = list;
						}
						list.Add(comp);
					}
				}
				// Cache comps by their type hierarchy
				foreach (var comp in comps)
				{
					var parentType = comp.GetType().BaseType;
					while (parentType != null && parentType != objType)
					{
						if (!_tmpCompsByType.TryGetValue(parentType, out var list))
						{
							list = SimplePool<List<ThingComp>>.Get();
							_tmpCompsByType[parentType] = list;
						}
						list.Add(comp);
						bloomFilter.Set(parentType);
						parentType = parentType.BaseType;
					}
				}
				compsByType.Clear();
				// Convert to Dictionary
				using (var enumerator = _tmpCompsByType.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						var pair = enumerator.Current;
						if (pair.Value.Count > 0)
						{
							compsByType[pair.Key] = pair.Value.ToArray();
						}
						pair.Value.Clear();
						SimplePool<List<ThingComp>>.Return(pair.Value);
					}
				}
				if (Verse_ThingWithComps_GetComp.Enabled)
				{
					thing.YaOptStruct() = new Verse_ThingWithComps_GetComp.YaOptThingWithCompsStruct
					{
						BloomFilter = bloomFilter,
						Equippable = TryGet<CompEquippable>(compsByType),
						CauseGameCondition = TryGet<CompCauseGameCondition>(compsByType),
						BladelinkWeapon = TryGet<CompBladelinkWeapon>(compsByType),
						PowerTrader = TryGet<CompPowerTrader>(compsByType),
						WakeUpDormant = TryGet<CompWakeUpDormant>(compsByType),
						AssignableToPawnGrave = TryGet<CompAssignableToPawn_Grave>(compsByType),
					};
				}
				_tmpCompsByType.Clear();
				var newVersion = hasThingHolder
						? VERSION_MAGICNUMBER_HAS_THINGHOLDER
						: VERSION_MAGICNUMBER_NO_THINGHOLDER;
				Volatile.Write(ref _listVersionFieldRef(comps), newVersion);
			}
		}

		private static T TryGet<T>(Dictionary<Type, ThingComp[]> compsByType) where T : class
		{
			if (compsByType.TryGetValue(typeof(T), out var list))
				return list[0] as T;
			return null;
		}

		[SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
		public static ThingComp Get(ThingWithComps thing, Type compType,
			List<ThingComp> compList, int version, Dictionary<Type, ThingComp[]> compsByType)
		{
			if (compList.Count == 0)
				return null;

			if (compsByType == null)
				return GetCompBySlowPath(compType, compList);

			if ((version & VERSION_MAGICNUMBER_MASK) != version)
			{
				lock (compsByType)
				{
					Interlocked.MemoryBarrier();
					version = Volatile.Read(ref _listVersionFieldRef(compList));
					if (version != VERSION_MAGICNUMBER_HAS_THINGHOLDER &&
						version != VERSION_MAGICNUMBER_NO_THINGHOLDER)
					{
						RecreateCompsByType(thing, compsByType, compList);
					}
					Interlocked.MemoryBarrier();
				}
			}

			var yaoptStruct = thing.YaOptStruct();
			if (compType == typeof(CompEquippable))
				return yaoptStruct.Equippable;
			if (!yaoptStruct.BloomFilter.Get(compType))
			{
				return null;
			}

			if (compsByType.TryGetValue(compType, out var list))
			{
				return list[0];
			}
			return null;
		}

		private static ThingComp GetCompBySlowPath(Type type, List<ThingComp> comps)
		{
			foreach (var comp in comps)
			{
				if (type.IsInstanceOfType(comp))
					return comp;
			}
			return null;
		}

		[SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
		public static void RetrieveThingHolderComps(ThingWithComps thing, List<IThingHolder> outThingsHolders)
		{
			var list = thing.AllComps;
			if (list.Count == 0)
				return;
			var listVersion = _listVersionFieldRef(list);
			if (listVersion == VERSION_MAGICNUMBER_NO_THINGHOLDER)
				return;

			var compsByType = _thingCompsByTypeFieldRef(thing);
			if (compsByType == null)
			{
				RetrieveThingHolderCompsBySlowPath(list, outThingsHolders);
				return;
			}

			if (listVersion != VERSION_MAGICNUMBER_HAS_THINGHOLDER)
			{
				lock (compsByType)
				{
					Interlocked.MemoryBarrier();
					listVersion = Volatile.Read(ref _listVersionFieldRef(list));
					if (listVersion != VERSION_MAGICNUMBER_HAS_THINGHOLDER &&
						listVersion != VERSION_MAGICNUMBER_NO_THINGHOLDER)
					{
						RecreateCompsByType(thing, compsByType, list);
					}
					Interlocked.MemoryBarrier();
				}
			}
			if (compsByType.TryGetValue(typeof(IThingHolder), out var result))
			{
				for (var i = 0; i < result.Length; i++)
				{
					outThingsHolders.Add((IThingHolder)result[i]);
				}
			}
		}

		private static void RetrieveThingHolderCompsBySlowPath(
			List<ThingComp> allComps, List<IThingHolder> outThingsHolders)
		{
			for (var i = 0; i < allComps.Count; i++)
			{
				if (allComps[i] is IThingHolder thingHolder)
				{
					outThingsHolders.Add(thingHolder);
				}
			}
		}
	}
}