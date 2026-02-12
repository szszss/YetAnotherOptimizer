using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Unity.Jobs;
using Verse;

namespace YaOpt.Helpers
{
	[StaticConstructorOnStartup]
	internal static class ParallelPreDrawHelper
	{
		public const int FAST_CPMU_CHECK_THRESHOLD = 8;
		public static bool FastRecacheRequestedAvailable { get; private set; } = true;
		public static int OverridenGetMaterialPropertyBlockTypeCount;
		public static Type[] OverridenGetMaterialPropertyBlockTypeArray;
		public static HashSet<Type> OverridenGetMaterialPropertyBlockTypes = new HashSet<Type>();
		public static HashSet<Type> OverridenPreDrawTypes = new HashSet<Type>(); // TODO: Unused, remove?
		public static JobHandle CullJobHandle;
		public static JobHandle PreDrawJobHandle;
		public static object Data;

		static ParallelPreDrawHelper()
		{
			TypeSearcher.RegisterSearchingType(typeof(PawnRenderNode), PawnRenderNode_RecacheRequested_Checker);
			TypeSearcher.RegisterSearchingType(typeof(PawnRenderNodeWorker), PawnRenderNodeWorker_GetMaterialPropertyBlock_Checker);
		}

		private static void PawnRenderNode_RecacheRequested_Checker(Type type)
		{
			if (type == typeof(PawnRenderNode))
				return;

			if (type.GetProperty(nameof(PawnRenderNode.RecacheRequested),
				    BindingFlags.Instance | BindingFlags.Public)?.GetMethod?.IsOverriden() == true)
			{
				YaOptMod.Warning($"Found a derived PawnRenderNode class ({type.FullName}) with overriding RecacheRequested. " +
				                 "FastRecacheRequested optimization is disabled now. " +
				                 "This is not an error. Just a compatibility measure.");
				FastRecacheRequestedAvailable = false;
			}
		}

		private static void PawnRenderNodeWorker_GetMaterialPropertyBlock_Checker(Type type)
		{
			if (type == typeof(PawnRenderNodeWorker))
				return;

			if (type.IsMethodOverriden(nameof(PawnRenderNodeWorker.GetMaterialPropertyBlock)))
			{
				YaOptMod.Debug("Found a derived PawnRenderNodeWorker class " +
				               $"with overriding GetMaterialPropertyBlock: {type.FullName}");
				OverridenGetMaterialPropertyBlockTypes.Add(type);
				OverridenGetMaterialPropertyBlockTypeCount++;
				OverridenGetMaterialPropertyBlockTypeArray = OverridenGetMaterialPropertyBlockTypes.ToArray();
			}

			if (type.IsMethodOverriden(nameof(PawnRenderNodeWorker.PreDraw)))
			{
				YaOptMod.Debug("Found a derived PawnRenderNodeWorker class " +
				               $"with overriding PreDraw: {type.FullName}");
				OverridenPreDrawTypes.Add(type);
			}
		}

		public static bool CanParallelMaterialUpdate(PawnRenderNodeWorker worker)
		{
			var type = worker.GetType();

			if (OverridenGetMaterialPropertyBlockTypeCount > FAST_CPMU_CHECK_THRESHOLD)
				return !OverridenGetMaterialPropertyBlockTypes.Contains(type);

			for (var i = 0; i < OverridenGetMaterialPropertyBlockTypeCount; i++)
			{
				if (type == OverridenGetMaterialPropertyBlockTypeArray[i])
					return false;
			}
			return true;
		}

		[MethodImpl(MethodImplOptions.NoInlining)] // For profiler
		public static void WaitUntilCullJobComplete()
		{
			CullJobHandle.CompleteWithSpinWait();
		}

		[MethodImpl(MethodImplOptions.NoInlining)] // For profiler
		public static void WaitUntilPreDrawJobComplete()
		{
			PreDrawJobHandle.CompleteWithSpinWait();
		}

		// The Profiler shows that 80% of root nodes have child nodes,
		// 35% of root nodes have grandchild nodes,
		// and 2.5% of root nodes have great-grandchild nodes.
		// Therefore, this method will inline the requestRecache check of child nodes and grandchild nodes,
		// and the deeper nodes will be handled by the original RecacheRequested.
		public static bool FastRecacheRequested(PawnRenderNode node)
		{
			if (!FastRecacheRequestedAvailable)
				return node.RecacheRequested;

			if (node.requestRecache)
				return true;

			if (node.children == null)
				return false;

			foreach (var child in node.children)
			{
				if (child.requestRecache)
					return true;

				if (child.children == null)
					continue;

				foreach (var grandChild in child.children)
				{
					if (grandChild.requestRecache)
						return true;

					if (grandChild.children == null)
						continue;

					foreach (var greatGrandChild in grandChild.children)
					{
						if (greatGrandChild.RecacheRequested)
							return true;
					}
				}
			}

			return false;
		}
	}
}