using HarmonyLib;
using System;
using YaOpt.Defines;
using YaOpt.Helpers;
using YaOpt.Helpers.ThreadSafe;

namespace YaOpt.Patches.ThreadSafe.Locked
{
	[ManualPatch]
	internal static class CompatibilityLockPatcher
	{
		static bool Patch(Harmony harmony)
		{
			if (!YaOptGlobal.NeedThreadSafe || CompatibilityDefines.LockPatches.Count == 0)
				return true;

			var noError = true;
			foreach (var request in CompatibilityDefines.LockPatches.Values)
			{
				try
				{
					YaOptMod.Debug($"Apply compatibility Lock patch for {request.TargetMethod.FullName()}");
					LockPatchManager.PatchMethod(harmony, request);
				}
				catch (Exception ex)
				{
					noError = false;
					YaOptMod.Error(ex.ToString());
				}
			}
			return noError;
		}
	}
}