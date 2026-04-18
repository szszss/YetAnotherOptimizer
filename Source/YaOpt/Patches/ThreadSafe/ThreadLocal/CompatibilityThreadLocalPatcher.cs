using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using YaOpt.Defines;
using YaOpt.Helpers;
using YaOpt.Helpers.ThreadLocal;

namespace YaOpt.Patches.ThreadSafe.ThreadLocal
{
	[ManualPatch]
	internal static class CompatibilityThreadLocalPatcher
	{
		static bool Patch(Harmony harmony)
		{
			if (!YaOptGlobal.NeedThreadSafe || CompatibilityDefines.ThreadLocalPatches.Count == 0)
				return true;

			var noError = true;
			var transpiler = new HarmonyMethod(typeof(CompatibilityThreadLocalPatcher), nameof(Transpiler));
			foreach (var method in CompatibilityDefines.ThreadLocalPatches.Keys)
			{
				try
				{
					harmony.Patch(method, transpiler: transpiler);
				}
				catch (Exception ex)
				{
					noError = false;
					YaOptMod.Error(ex.ToString());
				}
			}
			return noError;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
			ILGenerator generator, MethodBase method)
		{
			YaOptMod.Debug($"Apply compatibility ThreadLocal patch for {method.FullName()}");
			var enumerable = instructions;
			if (!CompatibilityDefines.ThreadLocalPatches.TryGetValue(method, out var fields))
				throw new Exception($"Cannot find fields to replace for {method.Name.ToStringSafe()}");
			foreach (var fieldName in fields)
			{
				enumerable = ThreadLocalHelper.ThreadLocalTranspiler(enumerable, generator, fieldName);
			}
			return enumerable;
		}
	}
}