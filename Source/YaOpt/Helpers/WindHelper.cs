using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace YaOpt.Helpers
{
	internal static class WindHelper
	{
		public static float CurrentWind;

		public static float LastWind = float.MinValue;

		public static Verse.WeakReference<Map> LastMap = new Verse.WeakReference<Map>(null);

		public static List<Material> PlantMaterials = new List<Material>();

		static WindHelper()
		{
			UpdateCallbackHelper.RegisterClearCacheCallback(ClearCache);
		}

		private static void ClearCache()
		{
			CurrentWind = 0;
			LastWind = float.MinValue;
			LastMap.Target = null;
		}

		public static void UpdateWindForMaterials()
		{
			var currentMap = Find.CurrentMap;
			if (currentMap == null)
				return;
			var shouldUpdate = false;
			if (LastMap.Target != currentMap)
			{
				LastMap.Target = currentMap;
				shouldUpdate = true;
			}
			if (!Mathf.Approximately(CurrentWind, LastWind))
			{
				LastWind = CurrentWind;
				shouldUpdate = true;
			}
			if (shouldUpdate)
			{
				for (var i = 0; i < PlantMaterials.Count; i++)
				{
					PlantMaterials[i].SetFloat(ShaderPropertyIDs.SwayHead, LastWind);
				}
			}
		}
	}
}