using HarmonyLib;
using System.Runtime.CompilerServices;
using Verse;
using YaOpt.Helpers;
using YaOpt.Helpers.Trampolines;

namespace YaOpt.Patches.Trampolines
{
	/// <summary>
	/// <seealso cref="YaOptSettings.OptThingGetComp"/>
	/// </summary>
	public abstract class Verse_ThingWithComps_GetComp : TrampolineInstaller
	{
		public static Verse_ThingWithComps_GetComp Instance;

		protected override void Prepare()
		{
			SourceMethod = AccessTools.Method(typeof(ThingWithComps), "GetComp", null, new[] { typeof(ThingComp) });
			TargetMethod = AccessTools.Method(typeof(GetCompHelper), nameof(GetCompHelper.Get));
			RuntimeHelpers.PrepareMethod(SourceMethod.MethodHandle);
		}

		protected override bool ShouldInstall()
		{
			return YaOptGlobal.Settings.OptThingGetComp.Enabled;
		}
	}
}