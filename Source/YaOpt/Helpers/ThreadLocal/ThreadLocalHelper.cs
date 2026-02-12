using System.Collections.Generic;
using Verse;

namespace YaOpt.Helpers.ThreadLocal
{
	public static class ThreadLocalHelper
	{
		public static List<T> NewList<T>() => new List<T>();

		public static Dictionary<K, V> NewDictionary<K, V>() => new Dictionary<K, V>();

		public static List<Thing> NewThingList() => new List<Thing>();

		public static HashSet<Thing> NewThingSet() => new HashSet<Thing>();

		public static List<Pawn> NewPawnList() => new List<Pawn>();
	}
}