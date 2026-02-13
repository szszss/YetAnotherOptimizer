using HarmonyLib;
using System;
using UnityEngine;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <summary>
	/// <seealso cref="YaOptSettings.OptSilhouette"/>
	/// </summary>
	[HarmonyPatch(typeof(SilhouetteUtility))]
	[HarmonyPatch("GetCachedSilhouetteData")]
	internal static class Verse_SilhouetteUtility_GetCachedSilhouetteData
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptSilhouette.Enabled;
		}

		static bool Prefix(Thing thing, ref ValueTuple<Mesh, Material> __result)
		{
			var key = SilhouetteHelper.GetKey(thing);
			if (!SilhouetteHelper.TryGetCache(key, out var cache))
			{
				Graphic coloredVersion;
				if (thing is Pawn pawn)
				{
					coloredVersion = pawn.Drawer.renderer.SilhouetteGraphic;
				}
				else
				{
					coloredVersion = thing.Graphic;
				}
				coloredVersion = coloredVersion.GetColoredVersion(ShaderDatabase.Silhouette, Color.white, Color.white);
				cache = SilhouetteHelper.AddCache(key, coloredVersion.MatEast, coloredVersion.MatWest);
			}

			if (thing.Rotation == Rot4.West)
			{
				__result = new ValueTuple<Mesh, Material>(MeshPool.GridPlaneFlip(Vector2.one), cache.west);
			}
			else
			{
				__result = new ValueTuple<Mesh, Material>(MeshPool.GridPlane(Vector2.one), cache.east);
			}

			return false;
		}
	}
}