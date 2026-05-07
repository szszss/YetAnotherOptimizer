using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using Verse;

namespace YaOpt.Helpers
{
	/// <summary>
	/// Caches texture lookups for Graphic_Multi initialization to avoid repeated path manipulation and content lookups.
	/// </summary>
	/// <seealso cref="YaOptSettings.OptGraphicTextureCache"/>
	public static class TextureCache
	{
		public const int TYPE_COLOR = 0;
		public const int TYPE_MASK = 1;
		public const int TYPE_MASK_CUSTOMPATH = 2;
		public const int VARIANT_DEFAULT = 0;
		public const int VARIANT_NORTH = 1;
		public const int VARIANT_EAST = 2;
		public const int VARIANT_SOUTH = 3;
		public const int VARIANT_WEST = 4;


		private static readonly Dictionary<string, CacheEntry> texCache = new Dictionary<string, CacheEntry>();

		private static readonly Dictionary<string, CacheEntry> texMaskCache = new Dictionary<string, CacheEntry>();

		private static CacheEntry lastUsedCacheEntry;

		private static string lastUsedPath = string.Empty;

		private static int lastUsedType = -1;

		[StructLayout(LayoutKind.Auto, Size = 64)]
		private class CacheEntry
		{
			[CanBeNull]
			public Verse.WeakReference<Texture2D> Default;

			[CanBeNull]
			public Verse.WeakReference<Texture2D> North;

			[CanBeNull]
			public Verse.WeakReference<Texture2D> East;

			[CanBeNull]
			public Verse.WeakReference<Texture2D> South;

			[CanBeNull]
			public Verse.WeakReference<Texture2D> West;

			private byte IsNullBitset;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public bool IsNull(int variant)
			{
				return (IsNullBitset & (1 << variant)) > 0;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void SetNull(int variant, bool isNull)
			{
				IsNullBitset = (byte)(IsNullBitset | ((isNull ? 1 : 0) << variant));
			}
		}

		static TextureCache()
		{
			UpdateCallbackHelper.RegisterClearCacheCallback(ClearCache);
		}

		private static void ClearCache()
		{
			texCache.Clear();
			texMaskCache.Clear();
			lastUsedCacheEntry = null;
			lastUsedPath = string.Empty;
			lastUsedType = -1;
		}

		/// <summary>
		/// Retrieves a texture variant from cache or loads it if not cached.
		/// </summary>
		public static Texture2D Get(string pathWithoutAnyPostfix, int type, int variant)
		{
			CacheEntry entry;
			if (lastUsedPath == pathWithoutAnyPostfix && lastUsedType == type)
			{
				entry = lastUsedCacheEntry;
			}
			else
			{
				switch (type)
				{
					case TYPE_COLOR:
						if (!texCache.TryGetValue(pathWithoutAnyPostfix, out entry))
						{
							entry = new CacheEntry();
							texCache[pathWithoutAnyPostfix] = entry;
						}
						break;
					case TYPE_MASK:
					case TYPE_MASK_CUSTOMPATH:
						if (!texMaskCache.TryGetValue(pathWithoutAnyPostfix, out entry))
						{
							entry = new CacheEntry();
							texMaskCache[pathWithoutAnyPostfix] = entry;
						}
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(type));
				}
				lastUsedCacheEntry = entry;
				lastUsedPath = pathWithoutAnyPostfix;
				lastUsedType = type;
			}
			var postfix = type == TYPE_MASK ? "m" : string.Empty;

			if (entry.IsNull(variant))
				return null;
			Verse.WeakReference<Texture2D> texRef;
			switch (variant)
			{
				case VARIANT_DEFAULT:
					if (type != TYPE_COLOR)
						return null;
					texRef = entry.Default;
					if (texRef == null || !texRef.IsAlive || texRef.Target == null)
						entry.Default = texRef = new Verse.WeakReference<Texture2D>(
							ContentFinder<Texture2D>.Get(pathWithoutAnyPostfix, false));
					break;
				case VARIANT_NORTH:
					texRef = entry.North;
					if (texRef == null || !texRef.IsAlive || texRef.Target == null)
						entry.North = texRef = new Verse.WeakReference<Texture2D>(
							ContentFinder<Texture2D>.Get(pathWithoutAnyPostfix + "_north" + postfix, false));
					break;
				case VARIANT_EAST:
					texRef = entry.East;
					if (texRef == null || !texRef.IsAlive || texRef.Target == null)
						entry.East = texRef = new Verse.WeakReference<Texture2D>(
							ContentFinder<Texture2D>.Get(pathWithoutAnyPostfix + "_east" + postfix, false));
					break;
				case VARIANT_SOUTH:
					texRef = entry.South;
					if (texRef == null || !texRef.IsAlive || texRef.Target == null)
						entry.South = texRef = new Verse.WeakReference<Texture2D>(
							ContentFinder<Texture2D>.Get(pathWithoutAnyPostfix + "_south" + postfix, false));
					break;
				case VARIANT_WEST:
					texRef = entry.West;
					if (texRef == null || !texRef.IsAlive || texRef.Target == null)
						entry.West = texRef = new Verse.WeakReference<Texture2D>(
							ContentFinder<Texture2D>.Get(pathWithoutAnyPostfix + "_west" + postfix, false));
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(variant));
			}

			entry.SetNull(variant, texRef.Target == null);
			return texRef.Target;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Texture2D GetDefault(string pathWithoutAnyPostfix, bool _)
		{
			return Get(pathWithoutAnyPostfix, TYPE_COLOR, VARIANT_DEFAULT);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Texture2D GetNorth(string pathWithoutAnyPostfix, bool _)
		{
			return Get(pathWithoutAnyPostfix, TYPE_COLOR, VARIANT_NORTH);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Texture2D GetEast(string pathWithoutAnyPostfix, bool _)
		{
			return Get(pathWithoutAnyPostfix, TYPE_COLOR, VARIANT_EAST);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Texture2D GetSouth(string pathWithoutAnyPostfix, bool _)
		{
			return Get(pathWithoutAnyPostfix, TYPE_COLOR, VARIANT_SOUTH);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Texture2D GetWest(string pathWithoutAnyPostfix, bool _)
		{
			return Get(pathWithoutAnyPostfix, TYPE_COLOR, VARIANT_WEST);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Texture2D GetNorthm(string pathWithoutAnyPostfix, bool _, bool needPostfixM)
		{
			var type = needPostfixM ? TYPE_MASK : TYPE_MASK_CUSTOMPATH;
			return Get(pathWithoutAnyPostfix, type, VARIANT_NORTH);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Texture2D GetEastm(string pathWithoutAnyPostfix, bool _, bool needPostfixM)
		{
			var type = needPostfixM ? TYPE_MASK : TYPE_MASK_CUSTOMPATH;
			return Get(pathWithoutAnyPostfix, type, VARIANT_EAST);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Texture2D GetSouthm(string pathWithoutAnyPostfix, bool _, bool needPostfixM)
		{
			var type = needPostfixM ? TYPE_MASK : TYPE_MASK_CUSTOMPATH;
			return Get(pathWithoutAnyPostfix, type, VARIANT_SOUTH);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Texture2D GetWestm(string pathWithoutAnyPostfix, bool _, bool needPostfixM)
		{
			var type = needPostfixM ? TYPE_MASK : TYPE_MASK_CUSTOMPATH;
			return Get(pathWithoutAnyPostfix, type, VARIANT_WEST);
		}
	}
}