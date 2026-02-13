using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace YaOpt.Helpers
{
	public static class MaterialColorCache
	{
		private class ObjectComparer : IEqualityComparer<Material>
		{
			public bool Equals(Material x, Material y)
			{
				return x == y;
			}

			public int GetHashCode(Material obj)
			{
				return obj.GetHashCode();
			}
		}

		private static readonly ConcurrentDictionary<Material, Color> cache = new ConcurrentDictionary<Material, Color>(new ObjectComparer());

		static MaterialColorCache()
		{
			UpdateCallbackHelper.RegisterClearCacheCallback(ClearCache);
		}

		private static void ClearCache()
		{
			cache.Clear();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool SetColor(Material material, in Color color)
		{
			if (cache.TryGetValue(material, out var oldColor))
			{
				if (oldColor == color)
					return false;
			}
			cache[material] = color;
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool GetColor(Material material, out Color color)
		{
			return cache.TryGetValue(material, out color);
		}
	}
}