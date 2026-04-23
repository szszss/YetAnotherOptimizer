using System;
using System.Linq;
using Prepatcher;
using YaOpt.Helpers;
using Instruction = Mono.Cecil.Cil.Instruction;
using ModuleDefinition = Mono.Cecil.ModuleDefinition;
using OpCodes = Mono.Cecil.Cil.OpCodes;

namespace YaOpt.Prepatch.Patches
{
	internal static class Verse_ContentFinder_Get
	{
		[FreePatch]
		static void RewriteAssembly(ModuleDefinition module)
		{
			var type = module.GetType("Verse", "ContentFinder`1");
			var method = MonoMod.Utils.Extensions.FindMethod(type, "Get");
			if (method == null)
				throw new MissingMemberException("Verse.ContentFinder", "Get");
			var fieldRefEnabled = module.ImportReference(
				typeof(YaOptPrepatch).GetField(nameof(YaOptPrepatch.ContentFinderGetEnabled)));
			var methodRefGetContent = module.ImportReference(
				typeof(ContentManager).GetMethod(nameof(ContentManager.GetContent)));
			var methodRefGetTypeFromHandle = module.ImportReference(
				typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle)));

			var processor = method.Body.GetILProcessor();
			var genericParameter = type.GenericParameters.First();
			Instruction insertTarget = method.Body.Instructions.First();

			var lable = processor.Create(OpCodes.Nop);
			// if (YaOptPrepatch.ContentFinderGetEnabled)
			processor.InsertBefore(insertTarget, Instruction.Create(OpCodes.Ldsfld, fieldRefEnabled));
			processor.InsertBefore(insertTarget, Instruction.Create(OpCodes.Brfalse_S, lable));
			//   return GetCompHelper.Get(typeof(T), itemPath, reportFailure);
			processor.InsertBefore(insertTarget, Instruction.Create(OpCodes.Ldtoken, genericParameter));
			processor.InsertBefore(insertTarget, Instruction.Create(OpCodes.Call, methodRefGetTypeFromHandle));
			processor.InsertBefore(insertTarget, Instruction.Create(OpCodes.Ldarg_0));
			processor.InsertBefore(insertTarget, Instruction.Create(OpCodes.Ldarg_1));
			processor.InsertBefore(insertTarget, Instruction.Create(OpCodes.Call, methodRefGetContent));
			processor.InsertBefore(insertTarget, Instruction.Create(OpCodes.Unbox_Any, genericParameter));
			processor.InsertBefore(insertTarget, Instruction.Create(OpCodes.Ret));
			processor.InsertBefore(insertTarget, lable);
		}
	}
}