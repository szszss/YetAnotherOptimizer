using Verse;

namespace YaOpt.Patches
{
	//[HarmonyPatch(typeof(Game))]
	//[HarmonyPatch(MethodType.Constructor)]
	//TODO: delete this
	internal static class Verse_TickManager_Constructor
	{
		static void Postfix(TickList ___tickListNormal)
		{
			//TickListHelper.NormalTickList = new WeakReference<TickList>(___tickListNormal);
		}
	}
}