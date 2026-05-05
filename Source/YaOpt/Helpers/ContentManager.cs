using HarmonyLib;
using LudeonTK;
using RimWorld.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
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
		public const int MAX_ATLAS_SIZE = 512;

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

		private struct OptimizedTextureInfo
		{
			public VirtualFile File;
			public int OriginalWidth;
			public int OriginalHeight;
			public int SkipLevels;
		}

		/// <summary>
		/// Keeps track of textures that were downsampled during load (Mipmap skipping) to save VRAM 
		/// when loading textures for StaticTextureAtlas.
		/// </summary>
		private static readonly Dictionary<int, OptimizedTextureInfo> _optimizedTextures =
			new Dictionary<int, OptimizedTextureInfo>();

		/// <summary>
		/// Keeps track of ThingDefs whose graphics were downsampled. Used during pawn or thing spawning 
		/// to quickly check if we need to restore their high-res textures.
		/// </summary>
		private static readonly HashSet<ThingDef> _optimizedThingDefs = new HashSet<ThingDef>();

		private static readonly Dictionary<ThingDef, List<Texture2D>> _thingDefToTexturesMapping =
			new Dictionary<ThingDef, List<Texture2D>>();

		private static readonly Queue<ThingDef> _thingDefReloadTextureQueue = new Queue<ThingDef>();

		private static bool _isGameStarting = true;

		private static bool _hasImageOpt;
		private static bool _hasGraphicsSettings;
		private static object _imageOptSettings;
		private static object _gsSettings;
		private static AccessTools.FieldRef<object, int> _imageOptAnisoLevel;
		private static AccessTools.FieldRef<object, float> _imageOptMipmapBias;
		private static AccessTools.FieldRef<object, float> _gsMipmapBias;

		public delegate bool LoadZstdDdsTextureDelegate(Texture2D texture,
			VirtualFile originalFile, string zstdFilePath);

		public static LoadZstdDdsTextureDelegate LoadZstdDdsTexture;

		private static readonly byte[] _tmpDdsHeaderBytes = new byte[128];
		private static readonly byte[] _tmpTextureDataBytes = new byte[(int)(512 * 512 * 4 * sizeof(int) * 1.34f)];
		private static GCHandle _tmpDdsHeaderHandle;

		private static int _statTotalTextureCount;
		private static int _statLoadedFullTextureCount;
		private static int _statLoadedDownsampledTextureCount;

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
		/// If <c>true</c>, downsampling preloading will be enabled.
		/// </summary>
		/// <remarks>
		/// Some textures in the game are forcibly loaded during startup to fill the texture atlas.
		/// With downsampling enabled, textures larger than 512 pixels will be downsampled to 512 pixels.
		/// The full-size texture will only be loaded when it is actually used in the game.
		/// </remarks>
		public static bool EnableDownsampling;

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
			UpdateCallbackHelper.RegisterPreRenderCallback(PreRender);

			_tmpDdsHeaderHandle = GCHandle.Alloc(_tmpDdsHeaderBytes, GCHandleType.Pinned);
			_hasImageOpt = YaOptGlobal.HasType("ImageOpt.ImageOpt");
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
			_hasGraphicsSettings = YaOptGlobal.HasType("GraphicSetter.GraphicSetter");
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

		private static void PreRender(int _)
		{
			if (!EnableDownsampling)
				return;

			while (_thingDefReloadTextureQueue.Count > 0)
			{
				var thingDef = _thingDefReloadTextureQueue.Dequeue();
				LoadFullResolutionTextureDo(thingDef);
			}
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
			_isGameStarting = false;
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
			// I didn't replace this IsInMainThread with the one in YaOptGlobal
			// because I'm not sure if this method will be called before YaOptGlobal.MarkAsMainThread or not.
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

		public static IEnumerable<T> EnsureAllLoaded<T>(IEnumerable<T> source) where T : class
		{
			foreach (var item in source)
			{
				if (item is Texture2D tex)
					MakeSureTextureLoaded(tex);
				yield return item;
			}
		}

		public static void RegisterTextureNotLoaded(int textureId, VirtualFile file)
		{
			_texturesNotLoaded[textureId] = file;
			_statTotalTextureCount++;
		}

		public static void MakeSureTextureLoaded(Texture2D texture)
		{
			if (texture is null)
				return;
			var id = texture.GetInstanceID();
			if (_texturesNotLoaded.Remove(id, out var file))
			{
				_statLoadedFullTextureCount++;
				LoadTexture(texture, file);
			}
		}

		public static void LoadTexture(Texture2D texture, VirtualFile file)
		{
			if (LoadZstdDdsTexture != null && file.FullPath != null)
			{
				var zstdFile = Path.ChangeExtension(file.FullPath, ".dds.zstd");
				if (File.Exists(zstdFile))
				{
					if (LoadZstdDdsTexture(texture, file, zstdFile))
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int GetMipmapDataSize(int width, int height, in DdsPixelFormat format)
		{
			if (format.IsCompressed)
			{
				int blockSize = format.IsDxt1 ? 8 : 16;
				return Math.Max(1, (width + 3) / 4) * Math.Max(1, (height + 3) / 4) * blockSize;
			}
			else
			{
				return width * height * (int)(format.RGBBitCount / 8);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool MayInsertIntoAtlas(Texture2D texture)
		{
			if (_optimizedTextures.TryGetValue(texture.GetInstanceID(), out var info))
			{
				return info.OriginalWidth < MAX_ATLAS_SIZE && info.OriginalHeight < MAX_ATLAS_SIZE;
			}
			return true;
		}

		public static void LoadFullResolutionTexture(Thing thing)
		{
			if (!EnableDownsampling || _optimizedThingDefs.Count == 0)
				return;

			var def = thing.def;
			if (def == null)
				return;

			if (_optimizedThingDefs.Remove(def))
			{
				_thingDefReloadTextureQueue.Enqueue(def);
			}
		}

		private static void LoadFullResolutionTextureDo(ThingDef thingDef)
		{
			if (_thingDefToTexturesMapping.TryGetValue(thingDef, out var list))
			{
				for (var i = 0; i < list.Count; i++)
				{
					var tex = list[i];
					CheckAndRestoreTexture(tex);
				}
				_thingDefToTexturesMapping.Remove(thingDef);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void CheckAndRestoreTexture(Texture2D tex)
		{
			if (tex != null && _optimizedTextures.Remove(tex.GetInstanceID(), out var info))
			{
				_statLoadedDownsampledTextureCount--;
				_statLoadedFullTextureCount++;
				LoadTexture(tex, info.File);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool CanDownsampleNow()
		{
			return EnableDownsampling && _isGameStarting && ContentFinderRequester.requester != null;
		}

		/// <summary>
		/// Analyzes the DDS header and determines if the texture should be downsampled during initial load.
		/// </summary>
		/// <remarks>
		/// RimWorld aggressively pre-loads HD textures for ThingDefs (like buildings) during startup just to bake them into StaticTextureAtlas.
		/// This consumes massive amounts of VRAM and RAM. 
		/// Since DDS files contain pre-computed mipmaps, we can skip the high-res mip levels and only read a smaller mipmap (e.g. 256x256),
		/// effectively tricking the game into loading a low-res version. We record the original sizes so we can restore them later when spawned.
		/// </remarks>
		public static bool TryCalculateDownsampleOffset(Texture2D texture, in DdsHeader ddsHeader,
			out ThingDef owner, out int skipLevels, out long offsetBytes)
		{
			owner = null;
			skipLevels = 0;
			offsetBytes = 0;

			owner = ContentFinderRequester.requester as ThingDef;
			if (owner == null)
				return false;

			if (ddsHeader.Width < MAX_ATLAS_SIZE && ddsHeader.Height < MAX_ATLAS_SIZE)
				return false;

			// Don't downsample the texture which the size is not multiple of 4
			if (ddsHeader.Width % 4 != 0 || ddsHeader.Height % 4 != 0)
				return false;

			int w = (int)ddsHeader.Width;
			int h = (int)ddsHeader.Height;
			int maxSkip = (int)ddsHeader.MipMapCount - 1;

			while ((w >= MAX_ATLAS_SIZE || h >= MAX_ATLAS_SIZE) && skipLevels < maxSkip)
			{
				var nextW = Math.Max(1, w / 2);
				var nextH = Math.Max(1, h / 2);

				// Fix "Compressed TextureFormat RGB(A) Compressed BC7 requires a texture size that is a multiple of 4"
				if (ddsHeader.PixelFormat.IsCompressed && (nextW % 4 != 0 || nextH % 4 != 0))
				{
					break;
				}

				offsetBytes += GetMipmapDataSize(w, h, ddsHeader.PixelFormat);
				w = nextW;
				h = nextH;
				skipLevels++;
			}

			if (skipLevels > 0)
			{
				return true;
			}

			return false;
		}

		public static void RegisterDownsampledTexture(ThingDef ownerDef, Texture2D texture, VirtualFile file, int originalWidth,
			int originalHeight, int skipLevels)
		{
			_optimizedTextures[texture.GetInstanceID()] = new OptimizedTextureInfo
			{
				File = file,
				OriginalWidth = originalWidth,
				OriginalHeight = originalHeight,
				SkipLevels = skipLevels
			};
			_optimizedThingDefs.Add(ownerDef);

			if (!_thingDefToTexturesMapping.TryGetValue(ownerDef, out var list))
			{
				list = new List<Texture2D>(1);
				_thingDefToTexturesMapping[ownerDef] = list;
			}
			list.Add(texture);
			_statLoadedFullTextureCount--; // Revert the increment in MakeSureTextureLoaded
			_statLoadedDownsampledTextureCount++;
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
				AssertDdsHeader(ddsHeader);
				if (ddsHeader.PixelFormat.IsBc7) // Actually it checks if the texture has Dx10 extension 
				{
					fs.Seek(20, SeekOrigin.Current);
				}

				ThingDef owner = null;
				int skipLevels = 0;
				long offset = 0;
				var downsampled = CanDownsampleNow() && TryCalculateDownsampleOffset(texture, ddsHeader,
									  out owner, out skipLevels, out offset);

				if (downsampled)
				{
					fs.Seek(offset, SeekOrigin.Current);
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
				if (ddsHeader.PixelFormat.IsCompressed && (ddsHeader.Width % 4 != 0 || ddsHeader.Height % 4 != 0))
				{
					YaOptMod.Warning($"The size of texture {file.Name}" +
									 $"({ddsHeader.Width}x{ddsHeader.Height}) " +
									 "is not multiple of 4. The texture could be glitch. " +
									 $"(Full path: {file.FullPath})");
					if (ddsHeader.Width % 4 != 0)
						ddsHeader.Width += 4 - (ddsHeader.Width % 4);
					if (ddsHeader.Height % 4 != 0)
						ddsHeader.Height += 4 - (ddsHeader.Height % 4);
				}

				var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
				try
				{
					UploadTextureDdsData(texture, ddsHeader, handle.AddrOfPinnedObject(), dataSize, skipLevels);
					if (downsampled)
					{
						RegisterDownsampledTexture(owner, texture, file,
							(int)ddsHeader.Width, (int)ddsHeader.Height, skipLevels);
					}
				}
				finally
				{
					handle.Free();
				}
			}
		}

		/// <summary>
		/// Upload the texture data with dds format to a texture.
		/// </summary>
		public static void UploadTextureDdsData(Texture2D texture, in DdsHeader ddsHeader, IntPtr dataPtr, int dataSize, int skipLevels = 0)
		{
			var currentWidth = (int)ddsHeader.Width;
			var currentHeight = (int)ddsHeader.Height;
			for (int i = 0; i < skipLevels; i++)
			{
				currentWidth = Math.Max(1, currentWidth / 2);
				currentHeight = Math.Max(1, currentHeight / 2);
			}

			var hasMipMap = (ddsHeader.Flags & DdsHeaderFlags.MipMapCount) != 0 && ddsHeader.MipMapCount > 1 + skipLevels;
			var pixelFormat = ddsHeader.PixelFormat;
			texture.Reinitialize(currentWidth, currentHeight, pixelFormat.ToTextureFormat(), hasMipMap);
			texture.LoadRawTextureData(dataPtr, dataSize);
			texture.filterMode = FilterMode.Trilinear;
			texture.anisoLevel = GetAnisoLevel();
			texture.mipMapBias = GetMipmapBias();
			texture.Apply(true, skipLevels == 0);
		}

		/// <summary>
		/// Load a non-dds image and upload the data.
		/// </summary>
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
					texture.Apply(true, false);
					var compressedTex = StaticTextureAtlas.FastCompressDXT(texture);
					if (compressedTex != texture) // FastCompressDXT don't compress small texture. It just return them.
					{
						var hasMipmap = compressedTex.mipmapCount > 1;
						texture.Reinitialize(compressedTex.width, compressedTex.height,
							compressedTex.format, hasMipmap);
						texture.Apply(false, true);
						for (int i = 0, j = compressedTex.mipmapCount; i < j; i++)
						{
							var mipmapWidth = Mathf.Max(1, compressedTex.width >> i);
							var mipmapHeight = Mathf.Max(1, compressedTex.height >> i);
							Graphics.CopyTexture(compressedTex, 0, i, 0, 0,
								mipmapWidth, mipmapHeight, texture, 0, i, 0, 0);
						}

						global::UnityEngine.Object.DestroyImmediate(compressedTex);
					}
					else
					{
						texture.Apply(true, true);
					}
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
		public static void AssertDdsHeader(in DdsHeader ddsHeader)
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

		[DebugOutput("YaOpt")]
		private static void PrintTextureStatistics()
		{
			if (_statTotalTextureCount == 0)
			{
				YaOptMod.Error("Unable to track texture usage when lazy loading is disabled.");
				return;
			}

			var unloaded = _statTotalTextureCount - _statLoadedFullTextureCount - _statLoadedDownsampledTextureCount;
			YaOptMod.Log($"Total: {_statTotalTextureCount}");
			var ratioLoaded = _statLoadedFullTextureCount / (float)_statTotalTextureCount * 100;
			var ratioUnloaded = 100f;
			if (!EnableDownsampling)
			{
				YaOptMod.Log($"Loaded: {_statLoadedFullTextureCount} ({ratioLoaded:F2}%)");
			}
			else
			{
				YaOptMod.Log($"Loaded (Full): {_statLoadedFullTextureCount} ({ratioLoaded:F2}%)");
				ratioUnloaded -= ratioLoaded;
				ratioLoaded = _statLoadedDownsampledTextureCount / (float)_statTotalTextureCount * 100;
				YaOptMod.Log($"Loaded (Down): {_statLoadedDownsampledTextureCount} ({ratioLoaded:F2}%)");
			}
			ratioUnloaded -= ratioLoaded;
			ratioUnloaded = Math.Max(ratioUnloaded, 0);
			YaOptMod.Log($"Unloaded : {unloaded} ({ratioUnloaded:F2}%)");
		}

		[DebugOutput("YaOpt")]
		private static void PrintTextureStatisticsWithDetails()
		{
			if (_statTotalTextureCount == 0)
			{
				YaOptMod.Error("Unable to track texture usage when lazy loading is disabled.");
				return;
			}

			PrintTextureStatistics();
			YaOptMod.Log("Now printing details:");

			var sb = new StringBuilder("Downsampled:");
			foreach (var (_, info) in _optimizedTextures)
			{
				sb.AppendLine().Append(info.File.Name).Append(" - ").Append(info.File.FullPath);
			}
			YaOptMod.Log(sb.ToString());

			sb = new StringBuilder("Unloaded:");
			foreach (var (_, file) in _texturesNotLoaded)
			{
				sb.AppendLine().Append(file.Name).Append(" - ").Append(file.FullPath);
			}
			YaOptMod.Log(sb.ToString());
		}
	}
}