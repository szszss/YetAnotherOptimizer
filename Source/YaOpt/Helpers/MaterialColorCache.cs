using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace YaOpt.Helpers
{
	/// <summary>
	/// Caches material color values to avoid expensive managed-to-native transitions.
	/// </summary>
	/// <remarks>
	/// Unity's <see cref="Material.color"/> property getter involves a managed-to-native transition
	/// to retrieve the color from the underlying C++ material. This overhead is significant when
	/// called frequently (e.g., thousands of times per frame during rendering).
	/// </remarks>
	/// <seealso cref="Patches.UnityEngine_Material_GetColor"/>
	/// <seealso cref="Patches.UnityEngine_Material_SetColor"/>
	/// <seealso cref="YaOptSettings.OptMaterialGetColor"/>
	public static class MaterialColorCache
	{
		/// <summary>
		/// Custom comparer for <see cref="Material"/> that uses reference equality.
		/// </summary>
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

		/// <summary>
		/// Thread-safe cache mapping materials to their cached colors.
		/// </summary>
		private static readonly ConcurrentDictionary<Material, Color> _cache = new ConcurrentDictionary<Material, Color>(new ObjectComparer());

		/// <summary>
		/// Static constructor that registers the cache clearing callback.
		/// </summary>
		static MaterialColorCache()
		{
			UpdateCallbackHelper.RegisterClearCacheCallback(ClearCache);
		}

		/// <summary>
		/// Clears the cache when changing maps or loading saves.
		/// </summary>
		private static void ClearCache()
		{
			_cache.Clear();
		}

		/// <summary>
		/// Updates the cached color for a material.
		/// </summary>
		/// <returns>
		/// <c>true</c> if the color changed and was updated;
		/// <c>false</c> if the color was the same (no update needed).
		/// </returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool SetColor(Material material, in Color color)
		{
			if (_cache.TryGetValue(material, out var oldColor))
			{
				if (oldColor == color)
					return false;
			}
			_cache[material] = color;
			return true;
		}

		/// <summary>
		/// Retrieves the cached color for a material.
		/// </summary>
		/// <returns><c>true</c> if a cached color was found; otherwise, <c>false</c>.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool GetColor(Material material, out Color color)
		{
			return _cache.TryGetValue(material, out color);
		}
	}
}