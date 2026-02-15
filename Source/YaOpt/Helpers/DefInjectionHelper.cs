using System.Collections.Generic;
using Verse;
using static Verse.DefInjectionPackage;

namespace YaOpt.Helpers
{
	internal static class DefInjectionHelper
	{
		private static readonly Dictionary<DefInjectionPackage, Dictionary<string, DefInjection>>
			AllNormalizedPathToInjectionMapping = new Dictionary<DefInjectionPackage, Dictionary<string, DefInjection>>();

		private static Dictionary<string, DefInjection> _currentMapping = null;

		public static void AddInjection(DefInjection defInjection)
		{
			//YaOptMod.Warning($"Add Inj: {defInjection.path} - {defInjection.normalizedPath} - {defInjection.injected}");
			if (defInjection.injected && !_currentMapping.ContainsKey(defInjection.normalizedPath))
				_currentMapping[defInjection.normalizedPath] = defInjection;
		}

		public static DefInjection CheckDuplicateInjection(
			string normalizedPath, string key)
		{
			if (_currentMapping.TryGetValue(normalizedPath, out var dupInj))
			{
				if (dupInj.injected && dupInj.path != key)
				{
					//YaOptMod.Warning($"Bad inj: {normalizedPath} {key}");
					return dupInj;
				}
			}
			return null;
		}

		public static void PrintError(DefInjection other, string path, string suggestedPath,
			List<string> errorList)
		{
			var text = "Duplicate def-injected translation key. Both " +
					   $"{other.path} and {path} refer to the same field ({suggestedPath})";
			if (other.path != other.nonBackCompatiblePath)
			{
				text += $" ({other.nonBackCompatiblePath} was auto-renamed to {other.path})";
			}
			text += $" ({other.fileSource})";
			errorList.Add(text);
		}

		public static void ChangeMapping(DefInjectionPackage package)
		{
			if (!AllNormalizedPathToInjectionMapping.TryGetValue(package, out var mapping))
			{
				mapping = new Dictionary<string, DefInjection>();
				AllNormalizedPathToInjectionMapping[package] = mapping;
			}
			_currentMapping = mapping;
		}

		public static void ClearCache()
		{
			AllNormalizedPathToInjectionMapping.Clear();
			_currentMapping = null;
		}
	}
}