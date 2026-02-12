using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse.AI;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <summary>
	/// <seealso cref="YaOptSettings.OptParallelPawnTick"/>
	/// </summary>
	[HarmonyPatch(typeof(JobDriver))]
	[HarmonyPatch(nameof(JobDriver.DriverTick))]
	internal static class Verse_AI_JobDriver_DriverTick
	{
		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelPawnTick.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var list = instructions.ToList();
			var success = false;

			// Change else if (!this.CheckCurrentToilEndOrFail())
			// To else if (!JobPredictor.ShouldCheckJobFail(this.pawn) || !this.CheckCurrentToilEndOrFail())
			for (var i = 0; i < list.Count; i++)
			{
				var instruction = list[i];

				if (instruction.opcode == OpCodes.Call && instruction.operand is MethodInfo methodInfo &&
					methodInfo.Name == "CheckCurrentToilEndOrFail")
				{
					var brfalse = list[i + 1];
					var leave = list[i + 2];
					if ((brfalse.opcode == OpCodes.Brfalse || brfalse.opcode == OpCodes.Brfalse_S) &&
						(leave.opcode == OpCodes.Leave || leave.opcode == OpCodes.Leave_S))
					{
						var labelGoOn = (Label)brfalse.operand;
						var newILs = new[]
						{
							CodeInstruction.LoadField(
								typeof(JobDriver),
								nameof(JobDriver.pawn)),
							CodeInstruction.Call(
								typeof(JobPredictor),
								nameof(JobPredictor.ShouldCheckJobFail)),
							new CodeInstruction(OpCodes.Brfalse_S, labelGoOn),
							CodeInstruction.LoadArgument(0)
						};
						list.InsertRange(i, newILs);
						success = true;
						break;
					}
				}
			}

			if (!success)
			{
				throw new Exception("Failed");
			}

			return list;
		}
	}
}