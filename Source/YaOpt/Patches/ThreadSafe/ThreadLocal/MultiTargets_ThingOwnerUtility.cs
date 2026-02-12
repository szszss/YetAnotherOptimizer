using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches.ThreadSafe.ThreadLocal
{
	[HarmonyPatch]
	internal static class MultiTargets_ThingOwnerUtility
	{
		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(ThingOwnerUtility), 
				nameof(ThingOwnerUtility.TryGetInnerInteractableThingOwner));
			yield return AccessTools.Method(typeof(ThingOwnerUtility),
				nameof(ThingOwnerUtility.GetAllThingsRecursively),
				new[] { typeof(IThingHolder), typeof(List<Thing>), typeof(bool), typeof(Predicate<IThingHolder>) });
		}

		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe || YaOptGlobal.Settings.OptParallelJobGiver.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
			ILGenerator generator, MethodBase methodBase)
		{
			LocalBuilder localTmpStack = null;
			LocalBuilder localTmpHolders = generator.DeclareLocal(typeof(List<IThingHolder>));
			
			yield return new CodeInstruction(OpCodes.Ldsfld, 
				AccessTools.Field(
					typeof(ThreadLocalThingOwnerUtility),
					nameof(ThreadLocalThingOwnerUtility.TmpHolders)));
			yield return new CodeInstruction(OpCodes.Call,
				AccessTools.PropertyGetter(typeof(ThreadLocal<List<IThingHolder>>), "Value"));
			yield return CodeInstruction.StoreLocal(localTmpHolders.LocalIndex);

			if (methodBase.Name == nameof(ThingOwnerUtility.GetAllThingsRecursively))
			{
				localTmpStack = generator.DeclareLocal(typeof(Stack<IThingHolder>));
				yield return new CodeInstruction(OpCodes.Ldsfld, 
					AccessTools.Field(
						typeof(ThreadLocalThingOwnerUtility),
						nameof(ThreadLocalThingOwnerUtility.TmpStack)));
				yield return new CodeInstruction(OpCodes.Call,
					AccessTools.PropertyGetter(typeof(ThreadLocal<Stack<IThingHolder>>), "Value"));
				yield return CodeInstruction.StoreLocal(localTmpStack.LocalIndex);
			}

			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Ldsfld && instruction.operand is FieldInfo fieldInfo)
				{
					if (fieldInfo.Name == "tmpStack" && localTmpStack != null)
					{
						yield return CodeInstruction.LoadLocal(localTmpStack.LocalIndex)
							.WithLabels(instruction.labels);
						continue;
					}
					else if (fieldInfo.Name == "tmpHolders")
					{
						yield return CodeInstruction.LoadLocal(localTmpHolders.LocalIndex)
							.WithLabels(instruction.labels);
						continue;
					}
				}
				yield return instruction;
			}
		}
	}
}