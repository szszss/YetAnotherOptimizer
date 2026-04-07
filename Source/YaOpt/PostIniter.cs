using Verse;
using YaOpt.Defines;
using YaOpt.Helpers;
using YaOpt.Patches;

namespace YaOpt
{
	[StaticConstructorOnStartup]
	internal static class PostIniter
	{
		static PostIniter()
		{
			YaOptGlobal.MarkAsMainThread();
			DefInjectionHelper.ClearCache();
			CompatibilityDefines.Load();
			ContentManager.PostInit();
			TypeSearcher.Init(); // Must init before patcher. Some patchers depend it.
			Patcher.Init();
			YaOptSubMod.PostInitAll(YaOptGlobal.SubMods);
			DebugHelper.Init();
		}
	}
}
