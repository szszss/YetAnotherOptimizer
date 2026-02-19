using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Unity.Jobs;
using Verse;

namespace YaOpt.Helpers
{
	/// <summary>
	/// Manages parallel render preparation for pawns and detects mod compatibility issues.
	/// </summary>
	/// <seealso cref="YaOptSettings.OptEarlyRenderPrepare"/>
	/// <seealso cref="YaOptSettings.OptParallelMaterialUpdate"/>
	/// <seealso cref="YaOptSettings.OptFastRecacheRequested"/>
	[StaticConstructorOnStartup]
	internal static class ParallelPreDrawHelper
	{
		/// <summary>
		/// Threshold for switching from linear search to hash set lookup in <see cref="CanParallelMaterialUpdate"/>.
		/// </summary>
		/// <remarks>
		/// When there are fewer overriding types, linear search through an array is faster
		/// than hash set lookup due to better cache locality.
		/// </remarks>
		public const int FAST_CPMU_CHECK_THRESHOLD = 8;

		/// <summary>
		/// Indicates whether the fast <see cref="FastRecacheRequested"/> method can be used.
		/// </summary>
		/// <remarks>
		/// Disabled if any mod overrides <see cref="PawnRenderNode.RecacheRequested"/> property,
		/// as the fast path only works with the vanilla implementation.
		/// </remarks>
		public static bool FastRecacheRequestedAvailable { get; private set; } = true;

		/// <summary>
		/// Number of <see cref="PawnRenderNodeWorker"/> types that override <see cref="PawnRenderNodeWorker.GetMaterialPropertyBlock"/>.
		/// </summary>
		public static int OverridenGetMaterialPropertyBlockTypeCount;

		/// <summary>
		/// Array of types that override <see cref="PawnRenderNodeWorker.GetMaterialPropertyBlock"/>.
		/// </summary>
		/// <remarks>
		/// Used for fast linear search when <see cref="OverridenGetMaterialPropertyBlockTypeCount"/> is low.
		/// </remarks>
		public static Type[] OverridenGetMaterialPropertyBlockTypeArray;

		/// <summary>
		/// Hash set of types that override <see cref="PawnRenderNodeWorker.GetMaterialPropertyBlock"/>.
		/// </summary>
		/// <remarks>
		/// Used for O(1) lookup when <see cref="OverridenGetMaterialPropertyBlockTypeCount"/> exceeds threshold.
		/// </remarks>
		public static HashSet<Type> OverridenGetMaterialPropertyBlockTypes = new HashSet<Type>();

		/// <summary>
		/// Hash set of types that override <see cref="PawnRenderNodeWorker.PreDraw"/>.
		/// </summary>
		/// <remarks>Currently unused but kept for potential future use.</remarks>
		public static HashSet<Type> OverridenPreDrawTypes = new HashSet<Type>(); // TODO: Unused, remove?

		/// <summary>
		/// Handle for the parallel camera culling job.
		/// </summary>
		public static JobHandle CullJobHandle;

		/// <summary>
		/// Handle for the parallel pre-draw job.
		/// </summary>
		public static JobHandle PreDrawJobHandle;

		/// <summary>
		/// Intermediate data is stored here (<c>NativeArray&lt;DynamicDrawManager.ThingCullDetails&gt;</c>).
		/// Since the type of this data is an inner class and cannot be directly referenced,
		/// it is boxed as an object type here.
		/// </summary>
		public static object Data;

		/// <summary>
		/// Static constructor that registers type searcher for compatibility detection.
		/// </summary>
		/// <remarks>
		/// Runs at startup to detect mods that override render-related methods,
		/// which may affect whether certain optimizations can be safely applied.
		/// </remarks>
		static ParallelPreDrawHelper()
		{
			TypeSearcher.RegisterSearchingType(typeof(PawnRenderNode), PawnRenderNode_RecacheRequested_Checker);
			TypeSearcher.RegisterSearchingType(typeof(PawnRenderNodeWorker), PawnRenderNodeWorker_GetMaterialPropertyBlock_Checker);
		}

		/// <summary>
		/// Checks if a <see cref="PawnRenderNode"/> derived type overrides <see cref="PawnRenderNode.RecacheRequested"/>.
		/// </summary>
		/// <remarks>
		/// If any override is found, <see cref="FastRecacheRequestedAvailable"/> is set to <c>false</c>
		/// because the fast path cannot handle custom logic.
		/// </remarks>
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

		/// <summary>
		/// Checks if a <see cref="PawnRenderNodeWorker"/> derived type overrides key methods.
		/// </summary>
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

		/// <summary>
		/// Determines if a worker's material property updates can be safely parallelized.
		/// </summary>
		/// <returns>
		/// <c>true</c> if the worker uses vanilla <see cref="PawnRenderNodeWorker.GetMaterialPropertyBlock"/>
		/// implementation (safe for parallel processing);
		/// <c>false</c> if overridden (may have thread-safety issues).
		/// </returns>
		/// <remarks>
		/// Uses a hybrid approach:
		/// <list type="bullet">
		/// <item>If fewer than <see cref="FAST_CPMU_CHECK_THRESHOLD"/> overriding types exist,
		/// uses linear array search (better cache locality).</item>
		/// <item>If more, uses hash set lookup (O(1) complexity).</item>
		/// </list>
		/// </remarks>
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

		/// <summary>
		/// Waits for the camera culling job to complete using spin-wait.
		/// </summary>
		/// <remarks>
		/// Spin-wait is used because the job should complete quickly, avoiding context switch overhead.
		/// </remarks>
		[MethodImpl(MethodImplOptions.NoInlining)] // For profiler
		public static void WaitUntilCullJobComplete()
		{
			CullJobHandle.CompleteWithSpinWait();
		}

		/// <summary>
		/// Waits for the pre-draw job to complete using spin-wait.
		/// </summary>
		/// <remarks>
		/// Spin-wait is used because the job should complete quickly, avoiding context switch overhead.
		/// </remarks>
		[MethodImpl(MethodImplOptions.NoInlining)] // For profiler
		public static void WaitUntilPreDrawJobComplete()
		{
			PreDrawJobHandle.CompleteWithSpinWait();
		}

		/// <summary>
		/// Fast alternative to <see cref="PawnRenderNode.RecacheRequested"/> that inlines child node checks.
		/// </summary>
		/// <returns><c>true</c> if any node in the tree needs recaching; otherwise, <c>false</c>.</returns>
		/// <remarks>
		/// <para>
		/// <b>Optimization Strategy:</b>
		/// Profiler data shows:
		/// <list type="bullet">
		/// <item>80% of root nodes have child nodes.</item>
		/// <item>35% of root nodes have grandchild nodes.</item>
		/// <item>2.5% of root nodes have great-grandchild nodes.</item>
		/// </list>
		/// This method inlines checks for the first three levels (covering 97.5% of cases)
		/// and falls back to the virtual property for deeper trees.
		/// </para>
		/// <para>
		/// <b>Fallback:</b> If <see cref="FastRecacheRequestedAvailable"/> is <c>false</c>,
		/// falls back to the vanilla property which may be overridden by mods.
		/// </para>
		/// </remarks>
		/// <seealso cref="YaOptSettings.OptFastRecacheRequested"/>
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