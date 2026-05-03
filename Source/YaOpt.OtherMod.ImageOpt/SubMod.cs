using RimWorld.IO;
using System;
using System.IO;
using UnityEngine;
using Verse;
using YaOpt.Helpers;
using ZstdSharp;

namespace YaOpt.OtherMod.ImageOpt
{
	/// <summary>
	/// Compatibility module for ImageOpt mod. Provides Zstd-compressed DDS texture loading.
	/// </summary>
	internal class SubMod : YaOptSubMod
	{
		public override void OnInit()
		{
			ContentManager.LoadZstdDdsTexture = LoadZstdDdsTexture;
		}

		private static unsafe bool LoadZstdDdsTexture(Texture2D texture,
			VirtualFile originalFile, string zstdFilePath)
		{
			try
			{
				var zstdBytes = File.ReadAllBytes(zstdFilePath);
				using (var decompressor = new Decompressor())
				{
					var ddsBytes = decompressor.Unwrap(zstdBytes);
					if (ddsBytes.Length < 128)
						throw new InvalidDataException("Invalid Zstd DDS file");
					var header = ddsBytes.Slice(0, 128);
					DdsHeader ddsHeader;
					fixed (void* ddsHeaderPtr = header)
					{
						ddsHeader = *((DdsHeader*)ddsHeaderPtr);
					}
					ContentManager.AssertDdsHeader(ddsHeader);
					var offset = 128;
					if (ddsHeader.PixelFormat.IsBc7) // Actually it checks if the texture has Dx10 extension 
						offset += 20;

					ThingDef owner = null;
					int skipLevels = 0;
					long additionalOffset = 0;
					var downsampled = ContentManager.CanDownsampleNow() &&
									  ContentManager.TryCalculateDownsampleOffset(texture, ddsHeader,
										  out owner, out skipLevels, out additionalOffset);

					if (downsampled)
					{
						offset += (int)additionalOffset;
					}

					var data = ddsBytes.Slice(offset);
					if (ddsHeader.PixelFormat.IsBgr888 && !ddsHeader.PixelFormat.IsCompressed)
					{
						var stride = (int)(ddsHeader.PixelFormat.RGBBitCount / 8);
						for (var i = 0; i < data.Length; i += stride)
						{
							(data[i], data[i + 2]) = (data[i + 2], data[i]);
						}
					}
					if (ddsHeader.PixelFormat.IsCompressed && (ddsHeader.Width % 4 != 0 || ddsHeader.Height % 4 != 0))
					{
						YaOptMod.Warning($"The size of texture {originalFile.Name}" +
						                 $"({ddsHeader.Width}x{ddsHeader.Height}) " +
						                 "is not multiple of 4. The texture could be glitch. " +
						                 $"(Full path: {originalFile.FullPath})");
						if (ddsHeader.Width % 4 != 0)
							ddsHeader.Width += 4 - (ddsHeader.Width % 4);
						if (ddsHeader.Height % 4 != 0)
							ddsHeader.Height += 4 - (ddsHeader.Height % 4);
					}
					fixed (void* ptr = data)
					{
						ContentManager.UploadTextureDdsData(texture, ddsHeader, new IntPtr(ptr), data.Length, skipLevels);
					}

					if (downsampled)
					{
						ContentManager.RegisterDownsampledTexture(owner, texture, originalFile,
							(int)ddsHeader.Width, (int)ddsHeader.Height, skipLevels);
					}
				}
			}
			catch (Exception ex)
			{
				YaOptMod.Error($"Failed to load Zstd DDS {zstdFilePath}. Fallback to vanilla method.\n{ex}");
				return false;
			}
			return true;
		}
	}
}
