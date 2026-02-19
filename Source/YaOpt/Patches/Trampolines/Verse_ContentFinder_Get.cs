using HarmonyLib;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;
using YaOpt.Helpers;
using YaOpt.Helpers.Trampolines;

namespace YaOpt.Patches.Trampolines
{
	/// <summary>
	/// Trampoline installer for ContentFinder&lt;Texture2D&gt;.Get to enable lazy texture loading.
	/// </summary>
	/// <seealso cref="YaOptSettings.OptLazyTextureLoad"/>
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