using Verse;
using YaOpt.Helpers;
using YaOpt.Patches;

namespace YaOpt
{
	[StaticConstructorOnStartup]
	internal static class PostIniter
	{
		static PostIniter()
		{
			DefInjectionHelper.ClearCache();
			AccessHelper.Init();
			ThingHelper.Init();
			CompatibilityDef.Cache();
			ContentManager.PostInit();
			TypeSearcher.Init(); // Must init before patcher. Some patchers depend it.
			Patcher.Init();
			YaOptSubMod.PostInitAll(YaOptGlobal.SubMods);
		}
	}
}