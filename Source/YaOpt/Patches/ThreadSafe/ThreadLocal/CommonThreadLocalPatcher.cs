using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using YaOpt.Defines;
using YaOpt.Helpers.ThreadLocal;

namespace YaOpt.Patches.ThreadSafe.ThreadLocal
{
	internal static class CommonThreadLocalPatcher
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			return CompatibilityDefines.ThreadLocalPatches.Keys;
		}

		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe && CompatibilityDefines.ThreadLocalPatches.Count > 0;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
			ILGenerator generator, MethodBase method)
		{
			var enumerable = instructions;
			if (!CompatibilityDefines.ThreadLocalPatches.TryGetValue(method, out var fields))
				throw new Exception($"Cannot find fields to replace for {method.Name.ToStringSafe()}");
			var holderType = method.DeclaringType;
			if (holderType == null || holderType.IsStatic())
				holderType = typeof(object);
			foreach (var field in fields)
			{
				enumerable = ThreadLocalHelper.TmpListTranspiler(enumerable, generator, field, holderType);
			}
			return enumerable;
		}
	}
}