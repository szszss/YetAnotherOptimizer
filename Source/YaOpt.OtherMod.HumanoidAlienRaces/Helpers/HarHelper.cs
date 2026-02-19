using AlienRace;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.OtherMod.HumanoidAlienRaces.Helpers
{
	/// <summary>
	/// Caches HAR graphic requests to avoid repeated GraphicDatabase lookups.
	/// </summary>
	[StaticConstructorOnStartup]
	internal static class HarHelper
	{
		private static readonly Dictionary<GraphicRequest, Graphic> _graphicCache =
			new Dictionary<GraphicRequest, Graphic>();

		static HarHelper()
		{
			UpdateCallbackHelper.RegisterClearCacheCallback(ClearCache);
		}

		private static void ClearCache()
		{
			_graphicCache.Clear();
		}

		public static Graphic GetGraphic(string path, Shader shader,
			Vector2 drawSize, Color color, Color colorTwo, GraphicData data, string maskPath = null)
		{
			var key = new GraphicRequest(typeof(Graphic_Multi_RotationFromData),
				path, shader, drawSize, color, colorTwo, data, 0, null, maskPath);
			if (_graphicCache.TryGetValue(key, out var graphic))
				return graphic;

			graphic = GraphicDatabase.Get<Graphic_Multi_RotationFromData>(
				path, shader, drawSize, color, colorTwo, data, maskPath);
			_graphicCache[key] = graphic;
			return graphic;
		}
	}
}