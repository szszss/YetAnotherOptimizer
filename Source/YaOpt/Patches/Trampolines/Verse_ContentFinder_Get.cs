using HarmonyLib;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;
using YaOpt.Helpers;
using YaOpt.Helpers.Trampolines;

namespace YaOpt.Patches.Trampolines
{
	/// <summary>
	/// <seealso cref="YaOptSettings.OptLazyTextureLoad"/>
	/// </summary>
	public abstract class Verse_ContentFinder_Get : TrampolineInstaller
	{
		public static Verse_ContentFinder_Get Instance;

		protected override void Prepare()
		{
			SourceMethod = AccessTools.Method(typeof(ContentFinder<Texture2D>), "Get");
			TargetMethod = AccessTools.Method(typeof(ContentManager), nameof(ContentManager.GetContent));
			RuntimeHelpers.PrepareMethod(SourceMethod.MethodHandle);
		}

		protected override bool ShouldInstall()
		{
			return YaOptGlobal.Settings.OptLazyTextureLoad.Enabled;
		}
	}
}