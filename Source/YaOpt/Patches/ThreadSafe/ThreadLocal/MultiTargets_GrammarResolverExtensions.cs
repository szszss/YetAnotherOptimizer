using HarmonyLib;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.Patches.ThreadSafe.ThreadLocal
{
	[HarmonyPatch]
	internal static class MultiTargets_GrammarResolverExtensions
	{
		[SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
		private class FakeGrammarResolverExtensions
		{
		}

		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(GrammarResolverSimpleStringExtensions),
				nameof(GrammarResolverSimpleStringExtensions.Formatted),
				new[] { typeof(string), typeof(NamedArgument) });
			yield return AccessTools.Method(typeof(GrammarResolverSimpleStringExtensions),
				nameof(GrammarResolverSimpleStringExtensions.Formatted),
				new[] { typeof(string), typeof(NamedArgument), typeof(NamedArgument) });
			yield return AccessTools.Method(typeof(GrammarResolverSimpleStringExtensions),
				nameof(GrammarResolverSimpleStringExtensions.Formatted),
				new[] { typeof(string), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument) });
			yield return AccessTools.Method(typeof(GrammarResolverSimpleStringExtensions),
				nameof(GrammarResolverSimpleStringExtensions.Formatted),
				new[]
				{
					typeof(string), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument),
					typeof(NamedArgument)
				});
			yield return AccessTools.Method(typeof(GrammarResolverSimpleStringExtensions),
				nameof(GrammarResolverSimpleStringExtensions.Formatted),
				new[]
				{
					typeof(string), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument),
					typeof(NamedArgument), typeof(NamedArgument)
				});
			yield return AccessTools.Method(typeof(GrammarResolverSimpleStringExtensions),
				nameof(GrammarResolverSimpleStringExtensions.Formatted),
				new[]
				{
					typeof(string), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument),
					typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument)
				});
			yield return AccessTools.Method(typeof(GrammarResolverSimpleStringExtensions),
				nameof(GrammarResolverSimpleStringExtensions.Formatted),
				new[]
				{
					typeof(string), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument),
					typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument),
					typeof(NamedArgument)
				});
			yield return AccessTools.Method(typeof(GrammarResolverSimpleStringExtensions),
				nameof(GrammarResolverSimpleStringExtensions.Formatted),
				new[]
				{
					typeof(string), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument),
					typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument),
					typeof(NamedArgument), typeof(NamedArgument)
				});
			yield return AccessTools.Method(typeof(GrammarResolverSimpleStringExtensions),
				nameof(GrammarResolverSimpleStringExtensions.Formatted),
				new[] { typeof(string), typeof(NamedArgument[]) });
			yield return AccessTools.Method(typeof(GrammarResolverSimpleStringExtensions),
				nameof(GrammarResolverSimpleStringExtensions.Formatted),
				new[] { typeof(string), typeof(IEnumerable<NamedArgument>) });
			yield return AccessTools.Method(typeof(GrammarResolverSimpleStringExtensions),
				nameof(GrammarResolverSimpleStringExtensions.Formatted),
				new[] { typeof(TaggedString), typeof(IEnumerable<NamedArgument>) });
		}

		static bool Prepare()
		{
			return YaOptGlobal.Settings.OptParallelJobGiver.Enabled;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			LocalBuilder localArgsLabels = generator.DeclareLocal(typeof(List<string>));
			LocalBuilder localObjects = generator.DeclareLocal(typeof(List<string>));
			yield return CodeInstruction.Call(
				typeof(ThreadLocalTmpList<FakeGrammarResolverExtensions, string>),
				nameof(ThreadLocalTmpList<FakeGrammarResolverExtensions, string>.Get));
			yield return CodeInstruction.StoreLocal(localArgsLabels.LocalIndex);
			yield return CodeInstruction.Call(
				typeof(ThreadLocalTmpList<FakeGrammarResolverExtensions, object>),
				nameof(ThreadLocalTmpList<FakeGrammarResolverExtensions, object>.Get));
			yield return CodeInstruction.StoreLocal(localObjects.LocalIndex);

			foreach (var instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Ldsfld && instruction.operand is FieldInfo fieldInfo)
				{
					if (fieldInfo.Name == "argsLabels")
					{
						yield return CodeInstruction.LoadLocal(localArgsLabels.LocalIndex)
							.WithLabels(instruction.labels);
						continue;
					}
					else if (fieldInfo.Name == "argsObjects")
					{
						yield return CodeInstruction.LoadLocal(localObjects.LocalIndex)
							.WithLabels(instruction.labels);
						continue;
					}
				}
				yield return instruction;
			}
		}
	}
}