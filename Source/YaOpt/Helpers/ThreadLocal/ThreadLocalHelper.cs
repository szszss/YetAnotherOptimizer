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

		public static IEnumerable<CodeInstruction> TmpListTranspiler<K, V>(
			IEnumerable<CodeInstruction> instructions, ILGenerator generator,
			string fieldName)
		{
			return TmpListTranspiler(instructions, generator, fieldName, typeof(K), typeof(V));
		}

		public static IEnumerable<CodeInstruction> TmpListTranspiler(
			IEnumerable<CodeInstruction> instructions, ILGenerator generator,
			string fieldName, Type holderType)
		{
			var list = instructions.ToList();
			Type type = null;
			foreach (var instruction in list)
			{
				if (instruction.LoadsField(fieldName, out _, out _) &&
					instruction.operand is FieldInfo fieldInfo)
				{
					type = fieldInfo.FieldType.GenericTypeArguments[0];
					break;
				}
			}
			if (type == null)
				throw new Exception($"The type of {fieldName} cannot be determined.");
			return TmpListTranspiler(list, generator, fieldName, holderType, type);
		}

		public static IEnumerable<CodeInstruction> TmpListTranspiler(
			IEnumerable<CodeInstruction> instructions, ILGenerator generator,
			string fieldName, Type holderType, Type tmpListType)
		{
			var local = generator.DeclareLocal(typeof(List<>).MakeGenericType(tmpListType));
			yield return CodeInstruction.Call(
				typeof(ThreadLocalTmpList<,>).MakeGenericType(holderType, tmpListType), "Get");
			yield return CodeInstruction.StoreLocal(local.LocalIndex);
			foreach (var instruction in instructions)
			{
				if (instruction.LoadsField(fieldName, out var isStatic, out var byAddress))
				{
					var labels = instruction.labels;
					var blocks = instruction.blocks;
					if (byAddress)
						throw new Exception($"Doesn't support address access field {fieldName}");
					if (!isStatic)
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