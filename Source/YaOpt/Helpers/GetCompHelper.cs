using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Verse;

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

		public static Dictionary<Type, ThingComp[]> CreateCompsByType(List<ThingComp> comps)
		{
			var compsByType = new Dictionary<Type, ThingComp[]>();
			RecreateCompsByType(compsByType, comps);
			return compsByType;
		}

		public static void RecreateCompsByType(
			Dictionary<Type, ThingComp[]> compsByType, List<ThingComp> comps)
		{
			lock (_tmpCompsByType)
			{
				var objType = typeof(object);
				var hasThingHolder = false;
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
				}
				var interfaceThingHolder = typeof(IThingHolder);
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
						parentType = parentType.BaseType;
					}
				}
				compsByType.Clear();
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
				_tmpCompsByType.Clear();
				_listVersionFieldRef(comps) = hasThingHolder
						? VERSION_MAGICNUMBER_HAS_THINGHOLDER
						: VERSION_MAGICNUMBER_NO_THINGHOLDER;
			}
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
					version = _listVersionFieldRef(compList);
					if (version != VERSION_MAGICNUMBER_HAS_THINGHOLDER &&
						version != VERSION_MAGICNUMBER_NO_THINGHOLDER)
					{
						RecreateCompsByType(compsByType, compList);
					}
					Interlocked.MemoryBarrier();
				}
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
		public static ThingComp[] GetThingHolderComps(ThingWithComps thing)
		{
			var list = thing.AllComps;
			if (list.Count == 0)
				return Array.Empty<ThingComp>();
			var listVersion = _listVersionFieldRef(list);
			if (listVersion == VERSION_MAGICNUMBER_NO_THINGHOLDER)
				return Array.Empty<ThingComp>();

			var compsByType = _thingCompsByTypeFieldRef(thing);
			if (listVersion != VERSION_MAGICNUMBER_HAS_THINGHOLDER)
			{
				lock (compsByType)
				{
					Interlocked.MemoryBarrier();
					listVersion = _listVersionFieldRef(list);
					if (listVersion != VERSION_MAGICNUMBER_HAS_THINGHOLDER &&
						listVersion != VERSION_MAGICNUMBER_NO_THINGHOLDER)
					{
						RecreateCompsByType(compsByType, list);
					}
					Interlocked.MemoryBarrier();
				}
			}
			if (compsByType.TryGetValue(typeof(IThingHolder), out var result))
				return result;
			return Array.Empty<ThingComp>();
		}
	}
}