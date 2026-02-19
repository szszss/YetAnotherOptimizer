using HarmonyLib;
using RimWorld.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using Verse;

namespace YaOpt.Helpers
{
	/// <summary>
	/// Manages lazy loading of mod content (textures) to reduce startup time and VRAM usage.
	/// </summary>
	/// <seealso cref="YaOptSettings.OptLazyTextureLoad"/>
	/// <seealso cref="YaOpt.Patches.Trampolines.Verse_ContentFinder_Get"/>
	public static class ContentManager
	{
		/// <summary>
		/// List of mods that contain texture assets.
		/// </summary>
		/// <remarks>Populated during <see cref="PostInit"/> for fast lookups.</remarks>
		public static List<ModContentPack> ModsContainTexture = new List<ModContentPack>();

		/// <summary>
		/// List of mods that contain audio assets.
		/// </summary>
		public static List<ModContentPack> ModsContainAudio = new List<ModContentPack>();

		/// <summary>
		/// List of mods that contain string assets.
		/// </summary>
		public static List<ModContentPack> ModsContainString = new List<ModContentPack>();

		/// <summary>
		/// Maps content types to lists of mods containing that type.
		/// </summary>
		public static Dictionary<Type, List<ModContentPack>> ModsByContainType = new Dictionary<Type, List<ModContentPack>>()
		{
			{ typeof(Texture2D), ModsContainTexture },
			{ typeof(AudioClip), ModsContainAudio },
			{ typeof(string), ModsContainString },
		};

		private static readonly Dictionary<string, RouteSign> _textureRouteSigns = new Dictionary<string, RouteSign>();
		private static readonly Dictionary<string, RouteSign> _audioRouteSigns = new Dictionary<string, RouteSign>();
		private static readonly Dictionary<string, RouteSign> _stringRouteSigns = new Dictionary<string, RouteSign>();
		private static readonly Dictionary<string, RouteSign> _shaderRouteSigns = new Dictionary<string, RouteSign>();

		private static readonly Dictionary<int, VirtualFile> _texturesNotLoaded = new Dictionary<int, VirtualFile>();

		private static bool _hasImageOpt;
		private static bool _hasGraphicsSettings;
		private static object _imageOptSettings;
		private static object _gsSettings;
		private static AccessTools.FieldRef<object, int> _imageOptAnisoLevel;
		private static AccessTools.FieldRef<object, float> _imageOptMipmapBias;
		private static AccessTools.FieldRef<object, float> _gsMipmapBias;

		public delegate bool LoadZstdDdsTextureDelegate(Texture2D texture, string zstdFilePath);
		public static LoadZstdDdsTextureDelegate LoadZstdDdsTexture;

		private static readonly byte[] _tmpDdsHeaderBytes = new byte[128];
		private static readonly byte[] _tmpTextureDataBytes = new byte[(int)(512 * 512 * 4 * sizeof(int) * 1.34f)];
		private static GCHandle _tmpDdsHeaderHandle;

		/// <summary>
		/// If <c>true</c>, only DDS textures are lazily loaded; other formats load immediately.
		/// </summary>
		/// <remarks>
		/// DDS textures can be loaded directly into GPU memory without CPU-side processing,
		/// making them ideal for lazy loading. Other formats require Unity's image conversion
		/// which may cause stutter if loaded during gameplay.
		/// </remarks>
		public static bool OnlyLazilyLoadDds;

		/// <summary>
		/// Stores routing information for content lookups.
		/// </summary>
		/// <remarks>
		/// After the first lookup, caches the source (mod, resources, or bundle) for fast subsequent loads.
		/// </remarks>
		private struct RouteSign
		{
			private ModContentHolder<Texture2D> _modTextureSource;
			private ModContentHolder<AudioClip> _modAudioSource;
			private ModContentHolder<string> _modStringSource;
			private Source _loadSource;
			private AssetType _loadType;

			public object TryLoad(Type itemType, string itemPath)
			{
				object t = null;
				if (itemType == typeof(Texture2D))
					_loadType = AssetType.Texture;
				else if (itemType == typeof(AudioClip))
					_loadType = AssetType.Audio;
				else if (itemType == typeof(string))
					_loadType = AssetType.String;
				else if (itemType == typeof(Shader))
					_loadType = AssetType.Shader;

				//YaOptMod.Warning($"Try load {itemType.Name} from {itemPath}");

				_loadSource = Source.Missing;
				if (itemType != typeof(Shader) && ModsByContainType.TryGetValue(itemType, out var runningModsListForReading))
				{
					for (var i = runningModsListForReading.Count - 1; i >= 0; i--)
					{
						switch (_loadType)
						{
							case AssetType.Texture:
								_modTextureSource = runningModsListForReading[i].GetContentHolder<Texture2D>();
								t = _modTextureSource.Get(itemPath);
								break;
							case AssetType.Audio:
								_modAudioSource = runningModsListForReading[i].GetContentHolder<AudioClip>();
								t = _modAudioSource.Get(itemPath);
								break;
							case AssetType.String:
								_modStringSource = runningModsListForReading[i].GetContentHolder<string>();
								t = _modStringSource.Get(itemPath);
								break;
							default:
								Log.Error($"Mod lacks manager for asset type {itemType.Name}");
								break;
						}

						if (t != null)
						{
							if (_loadType == AssetType.Texture)
								MakeSureTextureLoaded((Texture2D)t);
							_loadSource = Source.Mod;
							return t;
						}
					}
				}

				switch (_loadType)
				{
					case AssetType.Texture:
						t = Resources.Load<Texture2D>(GenFilePaths.ContentPath<Texture2D>() + itemPath);
						break;
					case AssetType.Audio:
						t = Resources.Load<AudioClip>(GenFilePaths.ContentPath<AudioClip>() + itemPath);
						break;
				}
				if (t != null)
				{
					_loadSource = Source.Resources;
					return t;
				}

				switch (_loadType)
				{
					case AssetType.Texture:
						t = ContentFinder<Texture2D>.TryFindAssetInModBundles(itemPath);
						break;
					case AssetType.Audio:
						t = ContentFinder<AudioClip>.TryFindAssetInModBundles(itemPath);
						break;
					case AssetType.Shader:
						t = ContentFinder<Shader>.TryFindAssetInModBundles(itemPath);
						break;
				}
				if (t != null)
				{
					_loadSource = Source.Bundle;
					return t;
				}
				return null;
			}

			public object Load(string itemPath)
			{
				switch (_loadSource)
				{
					case Source.Mod:
						switch (_loadType)
						{
							case AssetType.Texture:
								var tex = _modTextureSource.Get(itemPath);
								MakeSureTextureLoaded(tex);
								return tex;
							case AssetType.Audio:
								return _modAudioSource.Get(itemPath);
							case AssetType.String:
								return _modStringSource.Get(itemPath);
						}
						break;
					case Source.Resources:
						switch (_loadType)
						{
							case AssetType.Texture:
								return Resources.Load<Texture2D>(GenFilePaths.ContentPath<Texture2D>() + itemPath);
							case AssetType.Audio:
								return Resources.Load<AudioClip>(GenFilePaths.ContentPath<AudioClip>() + itemPath); ;
						}
						break;
					case Source.Bundle:
						switch (_loadType)
						{
							case AssetType.Texture:
								return ContentFinder<Texture2D>.TryFindAssetInModBundles(itemPath);
							case AssetType.Audio:
								return ContentFinder<AudioClip>.TryFindAssetInModBundles(itemPath);
							case AssetType.Shader:
								return ContentFinder<Shader>.TryFindAssetInModBundles(itemPath);
						}
						break;
				}
				return null;
			}

			private enum Source : byte
			{
				Missing,
				Mod,
				Resources,
				Bundle
			}

			private enum AssetType : byte
			{
				Unknown,
				Texture,
				Audio,
				String,
				Shader
			}
		}

		/// <summary>
		/// Initializes the content manager during mod loading.
		/// </summary>
		/// <remarks>
		/// <para>
		/// Called during <see cref="YaOptMod"/> construction. At this point, ModContentPacks
		/// are not fully initialized, so temporary data is filled. <see cref="PostInit"/> 
		/// will replace this with accurate data.
		/// </para>
		/// <para>
		/// Also detects and integrates with ImageOpt and GraphicsSettings mods for texture settings.
		/// </para>
		/// </remarks>
		public static void Init()
		{
			_tmpDdsHeaderHandle = GCHandle.Alloc(_tmpDdsHeaderBytes, GCHandleType.Pinned);
			_hasImageOpt = YaOptGlobal.HasType("ImageOpt.TextureLoadPatch");
			if (_hasImageOpt)
			{
				try
				{
					var type = AccessTools.TypeByName("ImageOpt.Settings");
					_imageOptAnisoLevel = AccessTools.FieldRefAccess<int>(type, "aniso_level");
					_imageOptMipmapBias = AccessTools.FieldRefAccess<float>(type, "mipmap_bias");
				}
				catch (Exception ex)
				{
					YaOptMod.Error(ex.ToString());
					_hasImageOpt = false;
				}
			}
			_hasGraphicsSettings = YaOptGlobal.HasType("ImageOpt.TextureLoadPatch");
			if (_hasGraphicsSettings)
			{
				try
				{
					var type = AccessTools.TypeByName("GraphicSetter.SettingsGroup");
					_gsMipmapBias = AccessTools.FieldRefAccess<float>(type, "mipMapBias");
				}
				catch (Exception ex)
				{
					YaOptMod.Error(ex.ToString());
					_hasGraphicsSettings = false;
				}
			}

			/*
			 * During Mod Class initialization, the ModContentPack is not inited yet,
			 * so we cannot obtain the actual number of assets contained in each Mod.
			 *
			 * Therefore, temporary data are filled in here. In PostInit (called during
			 * the StaticConstructorOnStartup phase), the actual data will be filled in.
			 *
			 * Between these two Inits, all asset reading operations will follow
			 * the original logic - iterating through all ModContentPacks.
			 */
			ModsContainTexture.AddRange(LoadedModManager.RunningModsListForReading);
			ModsContainAudio.AddRange(LoadedModManager.RunningModsListForReading);
			ModsContainString.AddRange(LoadedModManager.RunningModsListForReading);
		}

		/// <summary>
		/// Finalizes initialization after all mods are loaded.
		/// </summary>
		/// <remarks>
		/// Called during StaticConstructorOnStartup phase. Replaces temporary mod lists
		/// with accurate lists based on actual content counts.
		/// </remarks>
		public static void PostInit()
		{
			ModsContainTexture.Clear();
			ModsContainAudio.Clear();
			ModsContainString.Clear();
			foreach (var contentPack in LoadedModManager.RunningModsListForReading)
			{
				var texHolder = contentPack.GetContentHolder<Texture2D>();
				if (texHolder.contentList.Count > 0)
				{
					ModsContainTexture.Add(contentPack);
				}
				var audioHolder = contentPack.GetContentHolder<AudioClip>();
				if (audioHolder.contentList.Count > 0)
				{
					ModsContainAudio.Add(contentPack);
				}
				var stringHolder = contentPack.GetContentHolder<string>();
				if (stringHolder.contentList.Count > 0)
				{
					ModsContainString.Add(contentPack);
				}
			}
		}

		/// <summary>
		/// Retrieves content by type and path, using cached routes for fast lookup.
		/// </summary>
		/// <param name="itemType">The type of content (Texture2D, AudioClip, string, or Shader).</param>
		/// <param name="itemPath">The path to the content item.</param>
		/// <param name="reportFailure">If <c>true</c>, logs an error when content is not found.</param>
		/// <returns>The loaded content, or <c>null</c> if not found.</returns>
		/// <remarks>
		/// <para>
		/// Search order:
		/// <list type="number">
		/// <item>Active mods (highest priority mod first).</item>
		/// <item>Unity Resources.</item>
		/// <item>Asset bundles.</item>
		/// </list>
		/// </para>
		/// <para>
		/// For textures, this method triggers lazy loading if the texture hasn't been loaded yet.
		/// </para>
		/// </remarks>
		public static object GetContent(Type itemType, string itemPath, bool reportFailure = true)
		{
			if (!UnityData.IsInMainThread)
			{
				Log.Error($"Tried to get a resource {itemPath} from a different thread. " +
						  "All resources must be loaded in the main thread.");
				return null;
			}

			object t = null;
			Dictionary<string, RouteSign> routeSigns = null;
			if (itemType == typeof(Texture2D))
				routeSigns = _textureRouteSigns;
			else if (itemType == typeof(AudioClip))
				routeSigns = _audioRouteSigns;
			else if (itemType == typeof(string))
				routeSigns = _stringRouteSigns;
			else if (itemType == typeof(Shader))
				routeSigns = _shaderRouteSigns;

			if (routeSigns != null)
			{
				if (routeSigns.TryGetValue(itemPath, out var routeSign))
				{
					t = routeSign.Load(itemPath);
				}
				else
				{
					routeSign = new RouteSign();
					t = routeSign.TryLoad(itemType, itemPath);
					routeSigns[itemPath] = routeSign;
				}
			}

			if (t == null && reportFailure)
			{
				var text = ((ContentFinderRequester.requester != null) ? (" for def '" + ContentFinderRequester.requester.defName + "'") : "");
				Log.Error($"Could not load {itemType.Name} at '{itemPath}'{text} in any active mod or in base resources.");
			}

			return t;
		}

		public static void RegisterTextureNotLoaded(int textureId, VirtualFile file)
		{
			_texturesNotLoaded[textureId] = file;
		}

		public static void MakeSureTextureLoaded(Texture2D texture)
		{
			if (texture is null)
				return;
			var id = texture.GetInstanceID();
			if (_texturesNotLoaded.TryGetValue(id, out var file))
			{
				_texturesNotLoaded.Remove(id);

				if (LoadZstdDdsTexture != null && file.FullPath != null)
				{
					var zstdFile = Path.ChangeExtension(file.FullPath, ".dds.zstd");
					if (File.Exists(zstdFile))
					{
						if (LoadZstdDdsTexture(texture, zstdFile))
							return;
					}
				}

				if (!file.Exists)
					return;

				if (file.Name.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
					LoadTextureDds(texture, file);
				else
					LoadTextureViaImageConversion(texture, file);
			}
		}

		private static void LoadTextureDds(Texture2D texture, VirtualFile file)
		{
			if (file.GetType().Name != "FilesystemFile")
				throw new NotSupportedException("ModDdsLoader only supports FilesystemFile types.");

			using (var fs = new FileStream(file.FullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				if (fs.Read(_tmpDdsHeaderBytes, 0, 128) != 128)
				{
					throw new InvalidDataException("Invalid DDS file");
				}
				var ddsHeader = Marshal.PtrToStructure<DdsHeader>(_tmpDdsHeaderHandle.AddrOfPinnedObject());
				CheckDdsHeader(ddsHeader);
				if (ddsHeader.PixelFormat.IsBc7) // Actually it checks if the texture has Dx10 extension 
				{
					fs.Seek(20, SeekOrigin.Current);
				}

				var dataSizeLong = fs.Length - fs.Position;
				if (dataSizeLong > int.MaxValue)
				{
					throw new NotSupportedException($"File {file.FullPath} too larger (>4GB)");
				}
				var dataSize = (int)dataSizeLong;
				byte[] data;
				if (dataSize > _tmpTextureDataBytes.LongLength)
				{
					data = new byte[dataSize];
				}
				else
				{
					data = _tmpTextureDataBytes;
				}
				// ReSharper disable once MustUseReturnValue
				fs.Read(data, 0, dataSize);

				if (ddsHeader.PixelFormat.IsBgr888 && !ddsHeader.PixelFormat.IsCompressed)
				{
					var stride = (int)(ddsHeader.PixelFormat.RGBBitCount / 8);
					for (var i = 0; i < dataSize; i += stride)
					{
						(data[i], data[i + 2]) = (data[i + 2], data[i]);
					}
				}

				var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
				try
				{
					LoadTextureDdsData(texture, ddsHeader, handle.AddrOfPinnedObject(), data.Length);
				}
				finally
				{
					handle.Free();
				}
			}
		}

		public static void CheckDdsHeader(in DdsHeader ddsHeader)
		{
			if (ddsHeader.Magic != DdsHeader.RequiredMagic)
			{
				throw new InvalidDataException(
					$"Invalid DDS magic number: {ddsHeader.Magic:X8}. Expected: {DdsHeader.RequiredMagic:X8}");
			}
			if (ddsHeader.Size != DdsHeader.RequiredSize)
			{
				throw new InvalidDataException(
					$"Invalid DDS header size: {ddsHeader.Size}. Expected: {DdsHeader.RequiredSize}");
			}
			if (ddsHeader.PixelFormat.Size != DdsPixelFormat.RequiredSize)
			{
				throw new InvalidDataException(
					$"Invalid DDS pixel format size: {ddsHeader.PixelFormat.Size}. Expected: {DdsPixelFormat.RequiredSize}");
			}
		}

		public static void LoadTextureDdsData(Texture2D texture, in DdsHeader ddsHeader, IntPtr dataPtr, int dataSize)
		{
			var hasMipMap = (ddsHeader.Flags & DdsHeaderFlags.MipMapCount) != 0 && ddsHeader.MipMapCount > 1;
			var pixelFormat = ddsHeader.PixelFormat;
			texture.Reinitialize((int)ddsHeader.Width, (int)ddsHeader.Height, pixelFormat.ToTextureFormat(), hasMipMap);
			texture.LoadRawTextureData(dataPtr, dataSize);
			texture.filterMode = FilterMode.Trilinear;
			texture.anisoLevel = GetAnisoLevel();
			texture.mipMapBias = GetMipmapBias();
			texture.Apply(true, true);
		}

		private static void LoadTextureViaImageConversion(Texture2D texture, VirtualFile file)
		{
			var anisoLevel = GetAnisoLevel();
			var mipmapBias = GetMipmapBias();
			var array = file.ReadAllBytes();
			texture.LoadImage(array);
			if ((texture.width < 4 || texture.height < 4 || !Mathf.IsPowerOfTwo(texture.width) ||
				 !Mathf.IsPowerOfTwo(texture.height)) && Prefs.TextureCompression)
			{
				var num = StaticTextureAtlas.CalculateMaxMipmapsForDxtSupport(texture);
				if (Prefs.LogVerbose)
				{
					Log.Warning($"Texture {file.Name} is being reloaded with reduced mipmap count " +
								$"(clamped to {num}) due to non-power-of-two dimensions: " +
								$"({texture.width}x{texture.height}). This will be slower to load, and will look " +
								"worse when zoomed out. Consider using a power-of-two " +
								"texture size instead.");
				}
				if (!UnityData.ComputeShadersSupported)
				{
					var tmpTex = new Texture2D(texture.width, texture.height, TextureFormat.Alpha8, num, false);
					tmpTex.LoadImage(array);
					texture.Reinitialize(tmpTex.width, tmpTex.height, tmpTex.format, tmpTex.mipmapCount > 1);
					texture.LoadRawTextureData(tmpTex.GetRawTextureData());
					global::UnityEngine.Object.DestroyImmediate(tmpTex);
				}
			}
			var flag = texture.width % 4 == 0 && texture.height % 4 == 0;
			if (flag && Prefs.TextureCompression)
			{
				if (!UnityData.ComputeShadersSupported)
				{
					texture.Compress(true);
					texture.filterMode = FilterMode.Trilinear;
					texture.anisoLevel = anisoLevel;
					texture.mipMapBias = mipmapBias;
					texture.Apply(true, true);
				}
				else
				{
					texture.filterMode = FilterMode.Trilinear;
					texture.anisoLevel = anisoLevel;
					texture.mipMapBias = mipmapBias;
					texture.Apply(true, true);
					texture = StaticTextureAtlas.FastCompressDXT(texture, true);
				}
			}
			else
			{
				texture.filterMode = FilterMode.Trilinear;
				texture.anisoLevel = anisoLevel;
				texture.mipMapBias = mipmapBias;
				texture.Apply(true, true);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int GetAnisoLevel()
		{
			if (_hasImageOpt)
			{
				if (_imageOptSettings == null)
				{
					_imageOptSettings = AccessTools.Field(
							AccessTools.TypeByName("ImageOpt.ImageOpt"), "settings")
						.GetValue(null);
				}
				return _imageOptAnisoLevel(_imageOptSettings);
			}
			else if (_hasGraphicsSettings)
			{
				return 1;
			}
			return 2;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static float GetMipmapBias()
		{
			if (_hasImageOpt)
			{
				if (_imageOptSettings == null)
				{
					_imageOptSettings = AccessTools.Field(
							AccessTools.TypeByName("ImageOpt.ImageOpt"), "settings")
						.GetValue(null);
				}
				return _imageOptMipmapBias(_imageOptSettings);
			}
			else if (_hasGraphicsSettings)
			{
				if (_gsSettings == null)
				{
					_gsSettings = AccessTools.Field(
							AccessTools.TypeByName("GraphicSetter.GraphicsSettings"), "mainSettings")
						.GetValue(null);
				}
				return _gsMipmapBias(_gsSettings);
			}
			return 0;
		}
	}
}