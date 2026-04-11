using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Verse;

namespace YaOpt.Helpers.ThreadLocal
{
	internal static class ThreadLocalThingOwnerUtility
	{
		public static ThreadLocal<Stack<IThingHolder>> TmpStack =
			new ThreadLocal<Stack<IThingHolder>>(() => new Stack<IThingHolder>());

		public static ThreadLocal<List<IThingHolder>> TmpHolders =
			new ThreadLocal<List<IThingHolder>>(() => new List<IThingHolder>());

		public static ThreadLocal<List<Thing>> TmpThings =
			new ThreadLocal<List<Thing>>(() => new List<Thing>());

		public static ThreadLocal<List<IThingHolder>> TmpMapChildHolders =
			new ThreadLocal<List<IThingHolder>>(() => new List<IThingHolder>());

		public static ThreadLocal<object[]> TmpParameters =
			new ThreadLocal<object[]>(() => new object[6]);

		private static readonly Dictionary<Type, MethodInfo> getAllThingsRecursivelyMethodCache =
			new Dictionary<Type, MethodInfo>();

		static ThreadLocalThingOwnerUtility()
		{
			UpdateCallbackHelper.RegisterClearCacheCallback(ClearCache);
		}

		private static void ClearCache()
		{
			TmpStack.Dispose();
			TmpStack = new ThreadLocal<Stack<IThingHolder>>(() => new Stack<IThingHolder>());
			TmpHolders.Dispose();
			TmpHolders = new ThreadLocal<List<IThingHolder>>(() => new List<IThingHolder>());
			TmpThings.Dispose();
			TmpThings = new ThreadLocal<List<Thing>>(() => new List<Thing>());
			TmpMapChildHolders.Dispose();
			TmpMapChildHolders = new ThreadLocal<List<IThingHolder>>(() => new List<IThingHolder>());
		}

		public static MethodInfo GetAllThingsRecursivelyFindGenericMethod(Type genericType)
		{
			lock (getAllThingsRecursivelyMethodCache)
			{
				if (!getAllThingsRecursivelyMethodCache.TryGetValue(genericType, out var methodInfo))
				{
					methodInfo = AccessTools.Method(
						typeof(ThreadLocalThingOwnerUtility),
						nameof(GetAllThingsRecursivelyGeneric)).MakeGenericMethod(genericType);
					getAllThingsRecursivelyMethodCache[genericType] = methodInfo;
				}
				return methodInfo;
			}
		}

		public static void GetAllThingsRecursively(Map map, ThingRequest request,
			List<Thing> outThings, bool allowUnreal = true, Predicate<IThingHolder> passCheck = null,
			bool alsoGetSpawnedThings = true)
		{
			outThings.Clear();
			if (alsoGetSpawnedThings)
			{
				outThings.AddRangeFast(map.listerThings.ThingsMatching(request));
			}
			var tmpMapChildHolders = TmpMapChildHolders.Value;
			var tmpThings = TmpThings.Value;
			tmpMapChildHolders.Clear();
			map.GetChildHolders(tmpMapChildHolders);
			foreach (var childHolder in tmpMapChildHolders)
			{
				tmpThings.Clear();
				ThingOwnerUtility.GetAllThingsRecursively(childHolder, tmpThings, allowUnreal, passCheck);
				foreach (var childThing in tmpThings)
				{
					if (request.Accepts(childThing))
					{
						outThings.Add(childThing);
					}
				}
			}
			YaOptMod.Warning($"GetAllThingsRecursively: There are {outThings.Count} outThings and {tmpMapChildHolders.Count} holders.");
			tmpThings.Clear();
			tmpMapChildHolders.Clear();
		}

		public static void GetAllThingsRecursivelyGeneric<T>(Map map, ThingRequest request,
			List<T> outThings, bool allowUnreal = true, Predicate<IThingHolder> passCheck = null,
			bool alsoGetSpawnedThings = true) where T : Thing
		{
			outThings.Clear();
			if (alsoGetSpawnedThings)
			{
				var list = map.listerThings.ThingsMatching(request);
				foreach (var thing in list)
				{
					if (thing is T genericThing)
					{
						outThings.Add(genericThing);
					}
				}
			}
			var tmpMapChildHolders = TmpMapChildHolders.Value;
			var tmpThings = TmpThings.Value;
			tmpMapChildHolders.Clear();
			map.GetChildHolders(tmpMapChildHolders);
			foreach (var childHolder in tmpMapChildHolders)
			{
				tmpThings.Clear();
				ThingOwnerUtility.GetAllThingsRecursively(childHolder, tmpThings, allowUnreal, passCheck);
				foreach (var childThing in tmpThings)
				{
					if (childThing is T genericThing && request.Accepts(genericThing))
					{
						outThings.Add(genericThing);
					}
				}
			}
			YaOptMod.Warning($"GetAllThingsRecursivelyGeneric: There are {outThings.Count} outThings and {tmpMapChildHolders.Count} holders.");
			tmpThings.Clear();
			tmpMapChildHolders.Clear();
		}
	}
}