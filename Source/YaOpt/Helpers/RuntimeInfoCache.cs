using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace YaOpt.Helpers
{
	internal static class RuntimeInfoCache
	{
		private static readonly HashSet<Assembly> cachedAssemblies = new HashSet<Assembly>();
		private static readonly Dictionary<string, Type> typesByName = new Dictionary<string, Type>();
		private static readonly Dictionary<string, Type> typesByFullName = new Dictionary<string, Type>();
		private static readonly Dictionary<Assembly, string> assemblyNames = new Dictionary<Assembly, string>();

		public static void TryUpdateCache(IEnumerable<Assembly> enumerable = null)
		{
			if (enumerable == null)
			{
				enumerable = AccessTools.AllAssemblies();
			}

			foreach (Assembly assembly in enumerable)
			{
				if (cachedAssemblies.Add(assembly))
				{
					try
					{
						foreach (var type in AccessTools.GetTypesFromAssembly(assembly))
						{
							var name = type.Name;
							var fullName = type.FullName;
							if (!string.IsNullOrWhiteSpace(name) && !typesByName.ContainsKey(name))
							{
								typesByName[name] = type;
							}
							if (!string.IsNullOrWhiteSpace(fullName) && !typesByFullName.ContainsKey(fullName))
							{
								typesByFullName[fullName] = type;
							}
						}
					}
					catch (Exception ex)
					{
						YaOptMod.Error("An error was thrown while caching the types from " +
						               $"assembly {assembly.FullName}. " +
						               "Any call to AccessTools.TypeByName may fail to " +
						               "retrieve the types in this assembly.");
					}
				}
			}
		}

		public static Type GetTypeByName(string name)
		{
			TryUpdateCache();
			if (typesByFullName.TryGetValue(name, out var type))
				return type;
			if (typesByName.TryGetValue(name, out type))
				return type;
			return null;
		}

		public static string GetCachedAssemblyName(Assembly assembly)
		{
			if (assemblyNames.TryGetValue(assembly, out var name))
				return name;
			return null;
		}

		public static void SetCachedAssemblyName(Assembly assembly, string name)
		{
			assemblyNames[assembly] = name;
		}
	}
}