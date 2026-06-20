using HarmonyLib;

namespace YaOpt.OtherMod.PerformanceOptimizer
{
	public class SubMod : YaOptSubMod
	{
		public override void OnInit()
		{
			ThreadSafeGetCompReplacements.Init();
		}

		public override bool OnPatch(Harmony harmony)
		{
			ThreadSafeGetCompReplacements.Enabled = YaOptGlobal.NeedThreadSafe;
			return true;
		}
	}
}
