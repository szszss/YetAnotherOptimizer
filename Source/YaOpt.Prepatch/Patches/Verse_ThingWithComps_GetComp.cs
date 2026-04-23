using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Prepatcher;
using Verse;
using YaOpt.Helpers;
using Instruction = Mono.Cecil.Cil.Instruction;
using ModuleDefinition = Mono.Cecil.ModuleDefinition;
using OpCodes = Mono.Cecil.Cil.OpCodes;

namespace YaOpt.Prepatch.Patches
{
	internal static class Verse_ThingWithComps_GetComp
	{
		[FreePatch]
		static void RewriteAssembly(ModuleDefinition module)
		{
			var type = module.GetType("Verse", "ThingWithComps");
			var method = MonoMod.Utils.Extensions.FindMethod(type, "GetComp");
			if (method == null)
				throw new MissingMemberException("Verse.ThingWithComps", "GetComp");

			var fieldRefEnabled = module.ImportReference(
				typeof(YaOptPrepatch).GetField(nameof(YaOptPrepatch.ThingWithCompsGetCompEnabled)));
			var fieldRefComps = module.ImportReference(
				typeof(ThingWithComps).GetField("comps", BindingFlags.Instance | BindingFlags.NonPublic));
			var fieldRefCompsByType = module.ImportReference(
				typeof(ThingWithComps).GetField("compsByType", BindingFlags.Instance | BindingFlags.NonPublic));
			var fieldRefListVersion = module.ImportReference(
				typeof(List<ThingComp>).GetField("_version", BindingFlags.Instance | BindingFlags.NonPublic));
			var methodRefGetComp = module.ImportReference(
				typeof(GetCompHelper).GetMethod(nameof(GetCompHelper.Get)));
			var methodRefGetTypeFromHandle = module.ImportReference(
				typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle)));

			var processor = method.Body.GetILProcessor();
			var genericParameter = method.GenericParameters.First();
			Instruction insertTarget = null;
			var retFound = false;
			foreach (var instruction in method.Body.Instructions)
			{
				if (instruction.opcode == OpCodes.Ret)
				{
					retFound = true;
				}
				else if (retFound && instruction.opcode == OpCodes.Ldarg_0)
				{
					insertTarget = instruction.next;
					break;
				}
			}
			if (insertTarget == null)
				throw new Exception("Cannot find inserting target");

			var lable = processor.Create(OpCodes.Nop);
			// if (YaOptPrepatch.ThingWithCompsGetCompEnabled)
			processor.InsertBefore(insertTarget, Instruction.Create(OpCodes.Ldsfld, fieldRefEnabled));
			processor.InsertBefore(insertTarget, Instruction.Create(OpCodes.Brfalse_S, lable));
			//   return GetCompHelper.Get(this, typeof(T), this.comps, this.comps._version, this.compsByType);
			processor.InsertBefore(insertTarget, Instruction.Create(OpCodes.Ldtoken, genericParameter));
			processor.InsertBefore(insertTarget, Instruction.Create(OpCodes.Call, methodRefGetTypeFromHandle));
			processor.InsertBefore(insertTarget, Instruction.Create(OpCodes.Ldarg_0));
			processor.InsertBefore(insertTarget, Instruction.Create(OpCodes.Ldfld, fieldRefComps));
			processor.InsertBefore(insertTarget, Instruction.Create(OpCodes.Dup));
			processor.InsertBefore(insertTarget, Instruction.Create(OpCodes.Ldfld, fieldRefListVersion));
			processor.InsertBefore(insertTarget, Instruction.Create(OpCodes.Ldarg_0));
			processor.InsertBefore(insertTarget, Instruction.Create(OpCodes.Ldfld, fieldRefCompsByType));
			processor.InsertBefore(insertTarget, Instruction.Create(OpCodes.Call, methodRefGetComp));
			processor.InsertBefore(insertTarget, Instruction.Create(OpCodes.Ret));
			processor.InsertBefore(insertTarget, lable);
		}
	}
}