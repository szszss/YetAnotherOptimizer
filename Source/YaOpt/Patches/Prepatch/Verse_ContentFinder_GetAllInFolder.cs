using Mono.Cecil;
using Prepatcher;
using System;
using System.Linq;
using YaOpt.Helpers;
using Instruction = Mono.Cecil.Cil.Instruction;
using ModuleDefinition = Mono.Cecil.ModuleDefinition;
using OpCodes = Mono.Cecil.Cil.OpCodes;

namespace YaOpt.Patches.Prepatch
{
	internal static class Verse_ContentFinder_GetAllInFolder
	{
		public static bool Enabled = false;

		[FreePatch]
		static void RewriteAssembly(ModuleDefinition module)
		{
			var type = module.GetType("Verse", "ContentFinder`1");
			var method = MonoMod.Utils.Extensions.FindMethod(type, "GetAllInFolder");
			if (method == null)
				throw new MissingMemberException("Verse.ContentFinder", "GetAllInFolder");
			var fieldRefEnabled = module.ImportReference(
				typeof(Verse_ContentFinder_GetAllInFolder).GetField(nameof(Enabled)));
			var methodRefEnsureAllLoaded = module.ImportReference(
				typeof(ContentManager).GetMethod(nameof(ContentManager.EnsureAllLoaded)));

			var genericInstance = new GenericInstanceMethod(methodRefEnsureAllLoaded);
			genericInstance.GenericArguments.Add(type.GenericParameters.First());

			var processor = method.Body.GetILProcessor();
			var rets = method.Body.Instructions
				.Where(i => i.OpCode == OpCodes.Ret)
				.ToList();

			foreach (var ret in rets)
			{
				processor.InsertBefore(ret,
					Instruction.Create(OpCodes.Ldsfld, fieldRefEnabled));
				processor.InsertBefore(ret,
					Instruction.Create(OpCodes.Brfalse_S, ret));
				processor.InsertBefore(ret,
					Instruction.Create(OpCodes.Call, genericInstance));
			}
		}
	}
}
