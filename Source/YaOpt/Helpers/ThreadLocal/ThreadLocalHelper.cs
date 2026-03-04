using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace YaOpt.Helpers.ThreadLocal
{
	public static class ThreadLocalHelper
	{
		public static List<T> NewList<T>() => new List<T>();

		public static Dictionary<K, V> NewDictionary<K, V>() => new Dictionary<K, V>();

		public static List<Thing> NewThingList() => new List<Thing>();

		public static HashSet<Thing> NewThingSet() => new HashSet<Thing>();

		public static List<Pawn> NewPawnList() => new List<Pawn>();

		public static IEnumerable<CodeInstruction> ThreadLocalTranspiler(
			IEnumerable<CodeInstruction> instructions, ILGenerator generator,
			string fieldName)
		{
			var list = instructions.ToList();
			FieldInfo field = null;
			foreach (var instruction in list)
			{
				if (instruction.LoadsField(fieldName, out _, out _) &&
					instruction.operand is FieldInfo fieldInfo)
				{
					field = fieldInfo;
					break;
				}
			}
			if (field == null)
				throw new Exception($"The type of {fieldName} cannot be determined.");
			return ThreadLocalTranspiler(list, generator, field);
		}

		public static IEnumerable<CodeInstruction> ThreadLocalTranspiler(
			IEnumerable<CodeInstruction> instructions, ILGenerator generator,
			FieldInfo field)
		{
			var local = generator.DeclareLocal(field.FieldType);
			var type = typeof(ThreadLocalAllocator<>).MakeGenericType(field.FieldType);
			var index = type.GetMethod("TryAllocate").Invoke(null, new object[] { field.FullName(), false });
			yield return new CodeInstruction(OpCodes.Ldc_I4, index);
			yield return CodeInstruction.Call(type, "Get");
			yield return CodeInstruction.StoreLocal(local.LocalIndex);
			foreach (var instruction in instructions)
			{
				if (instruction.LoadsField(field, true))
				{
					throw new Exception($"Doesn't support address access field {field.Name}");
				}
				if (instruction.LoadsField(field, false))
				{
					var labels = instruction.labels;
					var blocks = instruction.blocks;
					if (!field.IsStatic)
						yield return new CodeInstruction(OpCodes.Pop);
					yield return CodeInstruction.LoadLocal(local.LocalIndex)
						.WithLabels(labels).WithBlocks(blocks);
					continue;
				}
				yield return instruction;
			}
		}
	}
}