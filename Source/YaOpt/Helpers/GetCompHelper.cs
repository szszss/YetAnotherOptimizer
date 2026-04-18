using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Verse;

namespace YaOpt.Helpers
{
	internal static class GetCompHelper
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
		public static ThingComp Get(ThingWithComps thing, Type compType, int version, Dictionary<Type, ThingComp[]> compsByType)
		{
			/*Type compType = null;
			var print = !usedMrgctx.Contains(mrgctx);
			var ptrMonoGenericInst = Marshal.ReadIntPtr(mrgctx, 8);
			var ptrMonoType = Marshal.ReadIntPtr(ptrMonoGenericInst, 8);
			var ptrMonoClass = Marshal.ReadIntPtr(ptrMonoType);
			var ptrRuntimeInfo = Marshal.ReadIntPtr(ptrMonoClass, 208);
			var ptrVTB = IntPtr.Zero;
			var ptrType = IntPtr.Zero;
			if (ptrRuntimeInfo != IntPtr.Zero)
			{
				ptrVTB = Marshal.ReadIntPtr(ptrRuntimeInfo, 8);
				if (ptrVTB != IntPtr.Zero)
				{
					ptrType = Marshal.ReadIntPtr(ptrVTB, 24);
					if (ptrType != IntPtr.Zero)
					{
						compType = AsmHelper.TrampolineFactory.GetObjectFromPtr<Type>(ptrType);
					}
				}
			}
			if (print)
			{
				usedMrgctx.Add(mrgctx);
				YaOptMod.Log("MRGCTX: 0x" + mrgctx.ToString("X"));
				YaOptMod.Log("MRGCTX + 0x10: 0x" + Marshal.ReadIntPtr(mrgctx + 0x10).ToString("X"));
				YaOptMod.Log("MRGCTX + 0x18: 0x" + Marshal.ReadIntPtr(mrgctx + 0x18).ToString("X"));
				YaOptMod.Log("MRGCTX + 0x20: 0x" + Marshal.ReadIntPtr(mrgctx + 0x20).ToString("X"));
				YaOptMod.Log("MRGCTX + 0x28: 0x" + Marshal.ReadIntPtr(mrgctx + 0x28).ToString("X"));
				YaOptMod.Log("MonoGenericInst: 0x" + ptrMonoGenericInst.ToString("X"));
				YaOptMod.Log("MonoType: 0x" + ptrMonoType.ToString("X"));
				YaOptMod.Log("MonoClass: 0x" + ptrMonoClass.ToString("X"));
				YaOptMod.Log("RuntimeInfo: 0x" + ptrMonoClass.ToString("X"));
				if (ptrRuntimeInfo != IntPtr.Zero)
				{
					YaOptMod.Log("VTB: 0x" + ptrVTB.ToString("X"));
					if (ptrVTB != IntPtr.Zero)
					{
						YaOptMod.Log("Type: 0x" + ptrType.ToString("X"));
						if (compType != null)
						{
							YaOptMod.Log("Resolved type: " + compType.Name);
						}
					}
				}
			}*/
			//compType = AsmHelper.TrampolineFactory.GetObjectFromPtr<Type>(mrgctx + 0x28);
			//var cfPtr = AsmHelper.TrampolineFactory.GetObjectMemoryAddress(typeof(CompForbiddable));
			var compList = thing.AllComps;
			if (compType == null || compList.Count == 0)
				return null;
			/*switch (compList.Count)
			{
				case 0: return null;
				case 1:
					var comp = compList[0];
					return compType.IsInstanceOfType(comp) ? comp : null; // IsInstanceOfType is just too slow
			}*/

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