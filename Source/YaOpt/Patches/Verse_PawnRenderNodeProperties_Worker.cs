using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Verse;

namespace YaOpt.Patches
{
	/// <summary>
	/// <seealso cref="YaOptSettings.OptPRNRWorker"/>
	/// </summary>
	[HarmonyPatch(typeof(PawnRenderNodeProperties))]
	[HarmonyPatch(nameof(PawnRenderNodeProperties.Worker), MethodType.Getter)]
	internal static class Verse_PawnRenderNodeProperties_Worker
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptPRNRWorker.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var label = generator.DefineLabel();
			yield return CodeInstruction.LoadArgument(0);
			yield return CodeInstruction.LoadField(typeof(PawnRenderNodeProperties), "workerInt");
			yield return new CodeInstruction(OpCodes.Dup);
			yield return new CodeInstruction(OpCodes.Brtrue_S, label);
			yield return new CodeInstruction(OpCodes.Pop);
			yield return CodeInstruction.LoadArgument(0);
			yield return CodeInstruction.LoadArgument(0);
			yield return CodeInstruction.LoadField(typeof(PawnRenderNodeProperties), "workerClass");
			yield return CodeInstruction.Call(typeof(GenWorker<PawnRenderNodeWorker>), "Get", new[] { typeof(Type) });
			yield return CodeInstruction.StoreField(typeof(PawnRenderNodeProperties), "workerInt");
			yield return CodeInstruction.LoadArgument(0);
			yield return CodeInstruction.LoadField(typeof(PawnRenderNodeProperties), "workerInt");
			yield return new CodeInstruction(OpCodes.Ret).WithLabels(label);
		}
	}
}