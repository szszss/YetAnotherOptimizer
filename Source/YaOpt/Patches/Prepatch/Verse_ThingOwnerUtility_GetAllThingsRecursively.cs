using Mono.Cecil;
using Mono.Cecil.Cil;
using Prepatcher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Verse;
using YaOpt.Helpers.ThreadLocal;

namespace YaOpt.Patches.Prepatch
{
	internal static class Verse_ThingOwnerUtility_GetAllThingsRecursively
	{
		public static bool Enabled = false;

		[FreePatch]
		static void RewriteAssembly(ModuleDefinition module)
		{
			var thingOwnerType = module.GetType("Verse", "ThingOwnerUtility");
			MethodDefinition method = null;
			foreach (MethodDefinition m in thingOwnerType.Methods)
			{
				if (m.Name == "GetAllThingsRecursively" && m.HasGenericParameters)
				{
					method = m;
					break;
				}
			}
			if (method == null)
				throw new MissingMemberException("Verse.ThingOwnerUtility", "GetAllThingsRecursively");

			var processor = method.Body.GetILProcessor();

			var fieldTmpThings = thingOwnerType.Fields.First(f => f.Name == "tmpThings");
			var fieldTmpMapChildHolders = thingOwnerType.Fields.First(f => f.Name == "tmpMapChildHolders");

			var methodGetTmpThings = module.ImportReference(
				typeof(Verse_ThingOwnerUtility_GetAllThingsRecursively).GetMethod(
					nameof(GetTmpThings),
					BindingFlags.Static | BindingFlags.NonPublic));
			var methodGetTmpMapChildHolders = module.ImportReference(
				typeof(Verse_ThingOwnerUtility_GetAllThingsRecursively).GetMethod(
					nameof(GetTmpMapChildHolders),
					BindingFlags.Static | BindingFlags.NonPublic));

			var localTmpThings = new VariableDefinition(
				module.ImportReference(typeof(List<Thing>)));
			var localTmpMapChildHolders = new VariableDefinition(
				module.ImportReference(typeof(List<IThingHolder>)));
			method.Body.Variables.Add(localTmpThings);
			method.Body.Variables.Add(localTmpMapChildHolders);

			foreach (var instr in method.Body.Instructions)
			{
				if (instr.OpCode != OpCodes.Ldsfld)
					continue;

				var field = (FieldReference)instr.Operand;
				if (field.DeclaringType.FullName != "Verse.ThingOwnerUtility")
					continue;

				if (field.Name == "tmpThings")
				{
					instr.OpCode = OpCodes.Ldloc;
					instr.Operand = localTmpThings;
				}
				else if (field.Name == "tmpMapChildHolders")
				{
					instr.OpCode = OpCodes.Ldloc;
					instr.Operand = localTmpMapChildHolders;
				}
			}

			var first = method.Body.Instructions[0];
			var loadThings = Instruction.Create(OpCodes.Ldsfld, fieldTmpThings);
			var callThings = Instruction.Create(OpCodes.Call, methodGetTmpThings);
			var storeThings = Instruction.Create(OpCodes.Stloc, localTmpThings);
			var loadHolders = Instruction.Create(OpCodes.Ldsfld, fieldTmpMapChildHolders);
			var callHolders = Instruction.Create(OpCodes.Call, methodGetTmpMapChildHolders);
			var storeHolders = Instruction.Create(OpCodes.Stloc, localTmpMapChildHolders);

			processor.InsertBefore(first, loadThings);
			processor.InsertAfter(loadThings, callThings);
			processor.InsertAfter(callThings, storeThings);
			processor.InsertAfter(storeThings, loadHolders);
			processor.InsertAfter(loadHolders, callHolders);
			processor.InsertAfter(callHolders, storeHolders);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static List<Thing> GetTmpThings(List<Thing> tmpThings)
		{
			return Enabled ? ThreadLocalThingOwnerUtility.TmpThings.Value : tmpThings;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static List<IThingHolder> GetTmpMapChildHolders(List<IThingHolder> tmpMapChildHolders)
		{
			return Enabled ? ThreadLocalThingOwnerUtility.TmpMapChildHolders.Value : tmpMapChildHolders;
		}
	}
}