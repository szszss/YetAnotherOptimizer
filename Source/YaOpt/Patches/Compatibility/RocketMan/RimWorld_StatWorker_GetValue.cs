using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Threading;

namespace YaOpt.Patches.Compatibility.RocketMan
{
	[HarmonyPatch(typeof(StatWorker))]
	[HarmonyPatch(nameof(StatWorker.GetValue), typeof(StatRequest), typeof(bool))]
	[HarmonyBefore("Krkr.RocketMan")]
	internal static class RimWorld_StatWorker_GetValue
	{
		private static readonly object lockObj = new object();

		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe && YaOptGlobal.HasMod("Krkr.RocketMan");
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var codeMatcher = new CodeMatcher(instructions, generator);
			codeMatcher.MatchStartForward(
				CodeMatch.LoadsArgument(),
				CodeMatch.LoadsArgument(),
				CodeMatch.LoadsArgument(),
				CodeMatch.Calls(AccessTools.Method(
					typeof(StatWorker),
					nameof(StatWorker.GetValueUnfinalized))),
				CodeMatch.StoresLocal())
				.ThrowIfInvalid("CodeMatcher cannot find " +
								"'float valueUnfinalized = this.GetValueUnfinalized(req, applyPostProcess)'")
				.SetAndAdvance(OpCodes.Ldsfld,
					AccessTools.Field(
						typeof(RimWorld_StatWorker_GetValue),
					nameof(lockObj)))
				.InsertAndAdvance(
					CodeInstruction.Call(typeof(Monitor), nameof(Monitor.Enter),
						new[] { typeof(object) }),
					CodeInstruction.LoadArgument(0)
						.WithBlocks(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock)))
				.Advance(3)
				.InsertAfter(
					new CodeInstruction(CodeInstruction.LoadField(
						typeof(RimWorld_StatWorker_GetValue),
						nameof(lockObj))
						.WithBlocks(new ExceptionBlock(ExceptionBlockType.BeginFinallyBlock))),
					CodeInstruction.Call(typeof(Monitor), nameof(Monitor.Exit)),
					new CodeInstruction(OpCodes.Endfinally)
						.WithBlocks(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock))
					);
			return codeMatcher.Instructions();
		}
	}
}