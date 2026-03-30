using HarmonyLib;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches.ThreadSafe.Locked
{
	[ManualPatch]
	internal static class Verse_AutoSlaughterManager_AnimalsToSlaughter
	{
		static void Patch(Harmony harmony)
		{
			if (YaOptGlobal.NeedThreadSafe || YaOptGlobal.Settings.OptParallelJobGiver.Enabled)
			{
				var helperType = typeof(LockBoilerplate.Spin<AutoSlaughterManager>);
				harmony.Patch(AccessTools.PropertyGetter(
						typeof(AutoSlaughterManager),
						nameof(AutoSlaughterManager.AnimalsToSlaughter)),
					new HarmonyMethod(helperType, LockBoilerplate.ENTER),
					new HarmonyMethod(helperType, LockBoilerplate.EXIT));
			}
		}
	}
}