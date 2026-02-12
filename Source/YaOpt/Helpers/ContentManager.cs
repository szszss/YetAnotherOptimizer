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
	public static class ContentManager
	{
		public static List<ModContentPack> modsContainTexture = new List<ModContentPack>();
		public static List<ModContentPack> modsContainAudio = new List<ModContentPack>();
		public static List<ModContentPack> modsContainString = new List<ModContentPack>();
		public static Dictionary<Type, List<ModContentPack>> modsByContainType = new Dictionary<Type, List<ModContentPack>>()
		{
			{ typeof(Texture2D), modsContainTexture },
			{ typeof(AudioClip), modsContainAudio },
			{ typeof(string), modsContainString },
		};

		private static readonly Dictionary<string, RouteSign> textureRouteSigns = new Dictionary<string, RouteSign>();
		private static readonly Dictionary<string, RouteSign> audioRouteSigns = new Dictionary<string, RouteSign>();
		private static readonly Dictionary<string, RouteSign> stringRouteSigns = new Dictionary<string, RouteSign>();
		private static readonly Dictionary<string, RouteSign> shaderRouteSigns = new Dictionary<string, RouteSign>();

		private static readonly Dictionary<int, VirtualFile> texturesNotLoaded = new Dictionary<int, VirtualFile>();

		private static bool hasImageOpt;
		private static bool hasGraphicsSettings;
		private static object imageOptSettings;
		private static object gsSettings;
		private static AccessTools.FieldRef<object, int> imageOptAnisoLevel;
		private static AccessTools.FieldRef<object, float> imageOptMipmapBias;
		private static AccessTools.FieldRef<object, float> gsMipmapBias;

		public delegate bool LoadZstdDdsTextureDelegate(Texture2D texture, string zstdFilePath);
		public static LoadZstdDdsTextureDelegate LoadZstdDdsTexture;

		private static readonly byte[] tmpDdsHeaderBytes = new byte[128];
		private static readonly byte[] tmpTextureDataBytes = new byte[(int)(512 * 512 * 4 * sizeof(int) * 1.34f)];
		private static GCHandle tmpDdsHeaderHandle;

		public static bool OnlyLazilyLoadDds;

		private struct RouteSign
		{
			private ModContentHolder<Texture2D> ModTextureSource;
			private ModContentHolder<AudioClip> ModAudioSource;
			private ModContentHolder<string> ModStringSource;
			private Source LoadSource;
			private AssetType LoadType;

			public object TryLoad(Type itemType, string itemPath)
			{
				object t = null;
				if (itemType == typeof(Texture2D))
					LoadType = AssetType.Texture;
				else if (itemType == typeof(AudioClip))
					LoadType = AssetType.Audio;
				else if (itemType == typeof(string))
					LoadType = AssetType.String;
				else if (itemType == typeof(Shader))
					LoadType = AssetType.Shader;

				//YaOptMod.Warning($"Try load {itemType.Name} from {itemPath}");

				LoadSource = Source.Missing;
				if (itemType != typeof(Shader) && modsByContainType.TryGetValue(itemType, out var runningModsListForReading))
				{
					for (var i = runningModsListForReading.Count - 1; i >= 0; i--)
					{
						switch (LoadType)
						{
							case AssetType.Texture:
								ModTextureSource = runningModsListForReading[i].GetContentHolder<Texture2D>();
								t = ModTextureSource.Get(itemPath);
								break;
							case AssetType.Audio:
								ModAudioSource = runningModsListForReading[i].GetContentHolder<AudioClip>();
								t = ModAudioSource.Get(itemPath);
								break;
							case AssetType.String:
								ModStringSource = runningModsListForReading[i].GetContentHolder<string>();
								t = ModStringSource.Get(itemPath);
								break;
							default:
								Log.Error($"Mod lacks manager for asset type {itemType.Name}");
								break;
						}
					
						if (t != null)
						{
							if (LoadType == AssetType.Texture)
								MakeSureTextureLoaded((Texture2D)t);
							LoadSource = Source.Mod;
							return t;
						}
					}
				}

				switch (LoadType)
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
					LoadSource = Source.Resources;
					return t;
				}

				switch (LoadType)
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
					LoadSource = Source.Bundle;
					return t;
				}
				return null;
			}

			public object Load(string itemPath)
			{
				switch (LoadSource)
				{
					case Source.Mod:
						switch (LoadType)
						{
							case AssetType.Texture:
								var tex = ModTextureSource.Get(itemPath);
								MakeSureTextureLoaded(tex);
								return tex;
							case AssetType.Audio:
								return ModAudioSource.Get(itemPath);
							case AssetType.String:
								return ModStringSource.Get(itemPath);
						}
						break;
					case Source.Resources:
						switch (LoadType)
						{
							case AssetType.Texture:
								return Resources.Load<Texture2D>(GenFilePaths.ContentPath<Texture2D>() + itemPath);
							case AssetType.Audio:
								return Resources.Load<AudioClip>(GenFilePaths.ContentPath<AudioClip>() + itemPath);;
						}
						break;
					case Source.Bundle:
						switch (LoadType)
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

		public static void Init()
		{
			tmpDdsHeaderHandle = GCHandle.Alloc(tmpDdsHeaderBytes, GCHandleType.Pinned);
			hasImageOpt = YaOptGlobal.HasType("ImageOpt.TextureLoadPatch");
			if (hasImageOpt)
			{
				try
				{
					var type = AccessTools.TypeByName("ImageOpt.Settings");
					imageOptAnisoLevel = AccessTools.FieldRefAccess<int>(type, "aniso_level");
					imageOptMipmapBias = AccessTools.FieldRefAccess<float>(type, "mipmap_bias");
				}
				catch (Exception ex)
				{
					YaOptMod.Error(ex.ToString());
					hasImageOpt = false;
				}
			}
			hasGraphicsSettings = YaOptGlobal.HasType("ImageOpt.TextureLoadPatch");
			if (hasGraphicsSettings)
			{
				try
				{
					var type = AccessTools.TypeByName("GraphicSetter.SettingsGroup");
					gsMipmapBias = AccessTools.FieldRefAccess<float>(type, "mipMapBias");
				}
				catch (Exception ex)
				{
					YaOptMod.Error(ex.ToString());
					hasGraphicsSettings = false;
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
			modsContainTexture.AddRange(LoadedModManager.RunningModsListForReading);
			modsContainAudio.AddRange(LoadedModManager.RunningModsListForReading);
			modsContainString.AddRange(LoadedModManager.RunningModsListForReading);
		}

		public static void PostInit()
		{
			modsContainTexture.Clear();
			modsContainAudio.Clear();
			modsContainString.Clear();
			foreach (var contentPack in LoadedModManager.RunningModsListForReading)
			{
				var texHolder = contentPack.GetContentHolder<Texture2D>();
				if (texHolder.contentList.Count > 0)
				{
					modsContainTexture.Add(contentPack);
				}
				var audioHolder = contentPack.GetContentHolder<AudioClip>();
				if (audioHolder.contentList.Count > 0)
				{
					modsContainAudio.Add(contentPack);
				}
				var stringHolder = contentPack.GetContentHolder<string>();
				if (stringHolder.contentList.Count > 0)
				{
					modsContainString.Add(contentPack);
				}
			}
		}

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
				routeSigns = textureRouteSigns;
			else if (itemType == typeof(AudioClip))
				routeSigns = audioRouteSigns;
			else if (itemType == typeof(string))
				routeSigns = stringRouteSigns;
			else if (itemType == typeof(Shader))
				routeSigns = shaderRouteSigns;

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

			//YaOptMod.Warning($"Try load {itemType.Name} from {itemPath} return {t.ToStringSafe()}");

			if (t == null && reportFailure)
			{
				var text = ((ContentFinderRequester.requester != null) ? (" for def '" + ContentFinderRequester.requester.defName + "'") : "");
				Log.Error($"Could not load {itemType.Name} at '{itemPath}'{text} in any active mod or in base resources.");
			}

			return t;
		}

		public static void RegisterTextureNotLoaded(int textureId, VirtualFile file)
		{
			texturesNotLoaded[textureId] = file;
		}

		public static void MakeSureTextureLoaded(Texture2D texture)
		{
			if (texture is null)
				return;
			var id = texture.GetInstanceID();
			if (texturesNotLoaded.TryGetValue(id, out var file))
			{
				texturesNotLoaded.Remove(id);

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
			//YaOptMod.Warning($"Actullay load dds file {file.Name}");

			if (file.GetType().Name != "FilesystemFile")
				throw new NotSupportedException("ModDdsLoader only supports FilesystemFile types.");

			using (var fs = new FileStream(file.FullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				if (fs.Read(tmpDdsHeaderBytes, 0, 128) != 128)
				{
					throw new InvalidDataException("Invalid DDS file");
				}
				var ddsHeader = Marshal.PtrToStructure<DdsHeader>(tmpDdsHeaderHandle.AddrOfPinnedObject());
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
				if (dataSize > tmpTextureDataBytes.LongLength)
				{
					data = new byte[dataSize];
				}
				else
				{
					data = tmpTextureDataBytes;
				}
				// ReSharper disable once MustUseReturnValue
				fs.Read(data, 0, dataSize);

				if (ddsHeader.PixelFormat.IsBgr888 && !ddsHeader.PixelFormat.IsCompressed)
				{
					var stride = (int)(ddsHeader.PixelFormat.RGBBitCount / 8);
					for (var i = 0; i < dataSize; i+=stride)
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
			if (hasImageOpt)
			{
				if (imageOptSettings == null)
				{
					imageOptSettings = AccessTools.Field(
							AccessTools.TypeByName("ImageOpt.ImageOpt"), "settings")
						.GetValue(null);
				}
				return imageOptAnisoLevel(imageOptSettings);
			}
			else if (hasGraphicsSettings)
			{
				return 1;
			}
			return 2;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static float GetMipmapBias()
		{
			if (hasImageOpt)
			{
				if (imageOptSettings == null)
				{
					imageOptSettings = AccessTools.Field(
							AccessTools.TypeByName("ImageOpt.ImageOpt"), "settings")
						.GetValue(null);
				}
				return imageOptMipmapBias(imageOptSettings);
			}
			else if (hasGraphicsSettings)
			{
				if (gsSettings == null)
				{
					gsSettings = AccessTools.Field(
							AccessTools.TypeByName("GraphicSetter.GraphicsSettings"), "mainSettings")
						.GetValue(null);
				}
				return gsMipmapBias(gsSettings);
			}
			return 0;
		}
	}
}