using HarmonyLib;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches
{
	/// <summary>
	/// <seealso cref="Helpers.UpdateCallbackHelper"/>
	/// </summary>
	[HarmonyPatch(typeof(Game))]
	[HarmonyPatch(nameof(Game.UpdatePlay))]
	internal static class Verse_Game_UpdatePlay
	{
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				yield return instruction;

				if (instruction.opcode == OpCodes.Callvirt && instruction.operand is MethodInfo method)
				{
					if (method.Name == nameof(World.WorldUpdate))
					{
						yield return CodeInstruction.Call(
							typeof(UpdateCallbackHelper),
							nameof(UpdateCallbackHelper.PreRender));
					}
					else if (method.Name == nameof(GameInfo.GameInfoUpdate))
					{
						yield return CodeInstruction.Call(
							typeof(UpdateCallbackHelper),
							nameof(UpdateCallbackHelper.PostRender));
					}
				}
			}
		}
	}
}