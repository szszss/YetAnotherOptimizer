using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Verse;

namespace YaOpt.Helpers
{
	public static class TypeSearcher
	{
		private static readonly Dictionary<Type, Action<Type>> searchingTypeAndCallbacks = new Dictionary<Type, Action<Type>>();

		public static void Init()
		{
			LongEventHandler.QueueLongEvent(Process, "YaOpt.Loading.SearchingTypes".Translate(), false, ex =>
			{
				YaOptMod.Error($"Error while searching types: {ex}");
			});
		}

		public static void Process()
		{
			var watch = new Stopwatch();
			watch.Start();
			var types = searchingTypeAndCallbacks.Keys.ToArray();
			var count = types.Length;
			if (count == 0)
				return;

			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				try
				{
					foreach (var type in assembly.GetTypes())
					{
						for (var i = 0; i < count; i++)
						{
							var searchingType = types[i];
							if (searchingType.IsAssignableFrom(type))
							{
								YaOptMod.Debug($"Found a derived class of {searchingType.Name}: {type.FullName}");
								searchingTypeAndCallbacks[searchingType].Invoke(type);
							}
						}
					}
				}
				catch (Exception ex)
				{
					YaOptMod.Error(ex.ToString());
				}
			}
			watch.Stop();
			YaOptMod.Debug($"Searching types time cost: {watch.Elapsed.TotalMilliseconds}ms");
		}

		public static void RegisterSearchingType(Type type, Action<Type> callback)
		{
			searchingTypeAndCallbacks[type] = callback;
		}
	}
}