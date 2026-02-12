using HarmonyLib;
using RimWorld.IO;
using System;
using System.IO;
using UnityEngine;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches.Early
{
	/// <summary>
	/// <seealso cref="YaOptSettings.OptLazyTextureLoad"/>
	/// </summary>
	[HarmonyPatch(typeof(ModContentLoader<Texture2D>))]
	[HarmonyPatch("LoadTexture")]
	[EarlyPatch]
	internal static class Verse_ModContentLoader_LoadTexture
	{
		private static readonly byte[] EMPTY_TEX_DATA = { 0, 0, 0, 0, 0 };

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptLazyTextureLoad.Enabled;
		}

		[HarmonyPriority(2000)]
		static bool Prefix(VirtualFile __0, ref Texture2D __result)
		{
			__result = null;
			if (__0.Exists)
			{
				if (ContentManager.OnlyLazilyLoadDds)
				{
					var hasZstdFile = false;
					if (ContentManager.LoadZstdDdsTexture != null)
					{
						var zstdFilePath = Path.ChangeExtension(__0.FullPath, ".dds.zstd");
						hasZstdFile = File.Exists(zstdFilePath);
					}
					if (!hasZstdFile && !__0.Name.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
						return true;
				}
				var tex = new Texture2D(2, 2, TextureFormat.Alpha8, true);
				tex.LoadRawTextureData(EMPTY_TEX_DATA);
				ContentManager.RegisterTextureNotLoaded(tex.GetInstanceID(), __0);
				tex.name = Path.GetFileNameWithoutExtension(__0.Name);
				__result = tex;
			}
			return false;
		}
	}
}