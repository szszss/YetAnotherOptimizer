using HarmonyLib;
using System.Reflection;

namespace YaOpt.Patches.Compatibility.RomanceOnTheRim
{
	[HarmonyPatch]
	[HarmonyPriority(Priority.High)]
	internal static class RomanceOnTheRim_RomanticInteractionUtility_MakeLoverBubble
	{
		static MethodBase TargetMethod()
		{
			return AccessTools.Method(
				AccessTools.TypeByName("RomanceOnTheRim.RomanticInteractionUtility"),
				"MakeLoverBubble");
		}

		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe &&
				   YaOptGlobal.HasMod("telardo.romanceontherim");
		}

		static bool Prefix()
		{
			return YaOptGlobal.IsInMainThread;
		}
	}
}