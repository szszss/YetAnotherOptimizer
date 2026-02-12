using System;
using System.IO;
using UnityEngine;
using Verse;
using YaOpt.Helpers;
using ZstdSharp;

namespace YaOpt.OtherMod.ImageOpt
{
	internal class SubMod : YaOptSubMod
	{
		public override void OnInit()
		{
			ContentManager.LoadZstdDdsTexture = LoadZstdDdsTexture;
		}

		private static unsafe bool LoadZstdDdsTexture(Texture2D texture, string zstdFilePath)
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
					ContentManager.CheckDdsHeader(ddsHeader);
					var offset = 128;
					if (ddsHeader.PixelFormat.IsBc7) // Actually it checks if the texture has Dx10 extension 
						offset += 20;
					var data = ddsBytes.Slice(offset);
					if (ddsHeader.PixelFormat.IsBgr888 && !ddsHeader.PixelFormat.IsCompressed)
					{
						var stride = (int)(ddsHeader.PixelFormat.RGBBitCount / 8);
						for (var i = 0; i < data.Length; i += stride)
						{
							(data[i], data[i + 2]) = (data[i + 2], data[i]);
						}
					}
					fixed (void* ptr = data)
					{
						ContentManager.LoadTextureDdsData(texture, ddsHeader, new IntPtr(ptr), data.Length);
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
