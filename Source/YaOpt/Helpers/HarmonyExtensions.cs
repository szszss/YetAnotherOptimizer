using HarmonyLib;
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using YaOpt.Patches.Early;

namespace YaOpt.Helpers
{
	public static class HarmonyExtensions
	{
		public static bool TryPatchAll(this Harmony harmony, Assembly assembly, bool earlyPatch = false)
		{
			var noError = true;
			foreach (var type in AccessTools.GetTypesFromAssembly(assembly))
			{
				var isEarlyPatch = type.GetCustomAttribute<EarlyPatchAttribute>() != null;
				if (isEarlyPatch == earlyPatch && type.HasHarmonyAttribute())
				{
					try
					{
						harmony.CreateClassProcessor(type).Patch();
					}
					catch (Exception ex)
					{
						noError = false;
						YaOptMod.Error(ex.ToString());
					}
				}
			}
			return noError;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool LoadsField(this CodeInstruction code, string fieldName,
			bool isStatic = false, bool byAddress = false)
		{
			var opcode = code.opcode;
			var opcodeCheck = false;
			if (isStatic)
				if (byAddress)
					opcodeCheck = opcode == OpCodes.Ldsflda;
				else
					opcodeCheck = opcode == OpCodes.Ldsfld;
			else
				if (byAddress)
				opcodeCheck = opcode == OpCodes.Ldflda;
			else
				opcodeCheck = opcode == OpCodes.Ldfld;
			return opcodeCheck && code.operand is FieldInfo fieldInfo && fieldInfo.Name == fieldName;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool StoresField(this CodeInstruction code, string fieldName, bool isStatic = false)
		{
			var opcode = code.opcode;
			var opcodeCheck = false;
			if (isStatic)
				opcodeCheck = opcode == OpCodes.Stsfld;
			else
				opcodeCheck = opcode == OpCodes.Stfld;
			return opcodeCheck && code.operand is FieldInfo fieldInfo && fieldInfo.Name == fieldName;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Calls(this CodeInstruction code, string methodName)
		{
			var opcode = code.opcode;
			var opcodeCheck = opcode == OpCodes.Call || opcode == OpCodes.Callvirt;
			return opcodeCheck && code.operand is MethodInfo methodInfo && methodInfo.Name == methodName;
		}
	}
}