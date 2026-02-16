using RimWorld;
using System;
using System.Reflection;
using Verse;
using YaOpt.Helpers;
using YaOpt.Patches.Compatibility;
using YaOpt.Patches.Trampolines;

namespace YaOpt.Patches
{
	internal static class Patcher
	{
		private static bool hasPatched;

		public static void Init()
		{
			TrampolinePatcher.Init();
			LongEventHandler.QueueLongEvent(PatchOnStartup, "YaOpt.Loading.Patching".Translate(), false, null);
		}

		private static void PatchOnStartup()
		{
			TryPatch(true, false);
		}

		public static void CheckAndRePatchOnLoadGame()
		{
			TryPatch(false, true);
		}

		public static void TryPatch(bool force, bool changeLongEventTitle, bool updateOptionSnapshot = true)
		{
			if (!force && !YaOptGlobal.AnyOptionChanged())
				return;

			if (changeLongEventTitle)
			{
				LongEventHandler.SetCurrentEventText("YaOpt.Loading.Patching".Translate());
			}

			var noError = true;
			try
			{
				YaOptMod.Debug("Preparing to run patcher");
				var assembly = Assembly.GetExecutingAssembly();
				var harmony = YaOptGlobal.Harmony;

				if (hasPatched)
				{
					YaOptMod.Debug("Uninstalling exist patches...");
					harmony.UnpatchAll(harmony.Id);
					TrampolinePatcher.UninstallAll();
					YaOptSubMod.UnpatchAll(YaOptGlobal.SubMods, harmony);
					hasPatched = false;
				}

				YaOptMod.Debug("Patching...");
				noError &= harmony.TryPatchAll(assembly);
				TrampolinePatcher.InstallAll();
				noError &= YaOptSubMod.PatchAll(YaOptGlobal.SubMods, harmony);
				Unpatcher.Unpatch();
				Unpatcher.PatchAgain();
				hasPatched = true;
				YaOptMod.Debug("Patcher complated");
			}
			catch (Exception ex)
			{
				YaOptMod.Error(ex.ToString());
				noError = false;
			}
			if (!noError)
			{
				YaOptMod.Error("Error(s) happened while patching. The game may not run properly.");
				Messages.Message("YaOpt.Message.ErrorWhilePatching".Translate().ToString(),
					null, MessageTypeDefOf.CautionInput, false);
			}

			if (updateOptionSnapshot)
				YaOptGlobal.CreateOptionSnapshot();
		}
	}
}