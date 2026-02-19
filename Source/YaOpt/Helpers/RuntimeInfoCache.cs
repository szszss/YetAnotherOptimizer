using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace YaOpt.Helpers
{
	/// <summary>
	/// Caches type information from loaded assemblies for fast Harmony type lookups.
	/// </summary>
	/// <remarks>
	/// Speeds up AccessTools.TypeByName calls during Harmony patch processing.
	/// </remarks>
	/// <seealso cref="YaOptSettings.OptRuntimeInfoCache"/>
	internal static class RuntimeInfoCache
	{
		private static readonly HashSet<Assembly> _cachedAssemblies = new HashSet<Assembly>();
		private static readonly Dictionary<string, Type> _typesByName = new Dictionary<string, Type>();
		private static readonly Dictionary<string, Type> _typesByFullName = new Dictionary<string, Type>();
		private static readonly Dictionary<Assembly, string> _assemblyNames = new Dictionary<Assembly, string>();

		/// <summary>
		/// Updates the cache with types from the specified assemblies.
		/// </summary>
		public static void TryUpdateCache(IEnumerable<Assembly> enumerable = null)
		{
			if (enumerable == null)
			{
				enumerable = AccessTools.AllAssemblies();
			}

			foreach (Assembly assembly in enumerable)
			{
				if (_cachedAssemblies.Add(assembly))
				{
					try
					{
						foreach (var type in AccessTools.GetTypesFromAssembly(assembly))
						{
							var name = type.Name;
							var fullName = type.FullName;
							if (!string.IsNullOrWhiteSpace(name) && !_typesByName.ContainsKey(name))
							{
								_typesByName[name] = type;
							}
							if (!string.IsNullOrWhiteSpace(fullName) && !_typesByFullName.ContainsKey(fullName))
							{
								_typesByFullName[fullName] = type;
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

		/// <summary>
		/// Gets a type by its name or full name from the cache.
		/// </summary>
		public static Type GetTypeByName(string name)
		{
			TryUpdateCache();
			if (_typesByFullName.TryGetValue(name, out var type))
				return type;
			if (_typesByName.TryGetValue(name, out type))
				return type;
			return null;
		}

		/// <summary>
		/// Gets the cached name for an assembly.
		/// </summary>
		public static string GetCachedAssemblyName(Assembly assembly)
		{
			if (_assemblyNames.TryGetValue(assembly, out var name))
				return name;
			return null;
		}

		/// <summary>
		/// Sets the cached name for an assembly.
		/// </summary>
		public static void SetCachedAssemblyName(Assembly assembly, string name)
		{
			_assemblyNames[assembly] = name;
		}
	}
}