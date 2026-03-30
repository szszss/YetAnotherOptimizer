using HarmonyLib;
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Verse;
using YaOpt.Patches;
using YaOpt.Patches.Early;

namespace YaOpt.Helpers
{
	public static class HarmonyExtensions
	{
		public static bool TryPatchAll(this Harmony harmony, Assembly assembly,
			bool earlyPatch = false, bool permanentPatch = false)
		{
			var noError = true;
			foreach (var type in AccessTools.GetTypesFromAssembly(assembly))
			{
				var isEarlyPatch = type.GetCustomAttribute<EarlyPatchAttribute>() != null;
				var isPermanentPatch = type.GetCustomAttribute<PermanentPatchAttribute>() != null;
				if (isEarlyPatch == earlyPatch && isPermanentPatch == permanentPatch)
				{
					if (type.HasHarmonyAttribute())
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
					else if (type.HasAttribute<ManualPatchAttribute>())
					{
						try
						{
							var method = type.GetMethod("Patch",
								BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
							if (method == null)
								throw new MissingMethodException(type.FullName, "Patch");
							var returnValue = method.Invoke(null, new object[] { harmony });
							if (returnValue is bool returnBool)
								noError &= returnBool;
						}
						catch (Exception ex)
						{
							noError = false;
							YaOptMod.Error(ex.ToString());
						}
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
			{
				if (byAddress)
					opcodeCheck = opcode == OpCodes.Ldsflda;
				else
					opcodeCheck = opcode == OpCodes.Ldsfld;
			}
			else
			{
				if (byAddress)
					opcodeCheck = opcode == OpCodes.Ldflda;
				else
					opcodeCheck = opcode == OpCodes.Ldfld;
			}
			return opcodeCheck && code.operand is FieldInfo fieldInfo && fieldInfo.Name == fieldName;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool LoadsField(this CodeInstruction code, string fieldName,
			out bool isStatic, out bool byAddress)
		{
			isStatic = false;
			byAddress = false;
			var opcode = code.opcode;
			if (opcode == OpCodes.Ldsflda ||
				opcode == OpCodes.Ldsfld ||
				opcode == OpCodes.Ldflda ||
				opcode == OpCodes.Ldfld)
			{
				isStatic = opcode == OpCodes.Ldsflda || opcode == OpCodes.Ldsfld;
				byAddress = opcode == OpCodes.Ldsflda || opcode == OpCodes.Ldflda;
				return code.operand is FieldInfo fieldInfo && fieldInfo.Name == fieldName;
			}
			return false;
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