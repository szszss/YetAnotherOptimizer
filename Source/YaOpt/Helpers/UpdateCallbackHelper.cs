using System;
using System.Collections.Generic;
using Verse;
// ReSharper disable InconsistentlySynchronizedField

namespace YaOpt.Helpers
{
	/// <summary>
	/// Provides callbacks for game update lifecycle events (tick, render, cache clear).
	/// </summary>
	public static class UpdateCallbackHelper
	{
		/// <summary>
		/// Callback invoked with the current game tick.
		/// </summary>
		public delegate void UpdateCallback(int tick);

		/// <summary>
		/// Callback invoked when caches should be cleared.
		/// </summary>
		public delegate void ClearCacheCallback();

		private static readonly List<UpdateCallback> preTickMethods = new List<UpdateCallback>();

		private static readonly List<UpdateCallback> postTickMethods = new List<UpdateCallback>();

		private static readonly List<UpdateCallback> preRenderMethods = new List<UpdateCallback>();

		private static readonly List<UpdateCallback> postRenderMethods = new List<UpdateCallback>();

		private static readonly List<UpdateCallback> preDynamicDrawMethods = new List<UpdateCallback>();

		private static readonly List<ClearCacheCallback> clearCacheMethods = new List<ClearCacheCallback>();

		/// <summary>
		/// Registers a callback to be invoked before each game tick.
		/// </summary>
		public static void RegisterPreTickCallback(UpdateCallback callback)
		{
			lock (preTickMethods)
			{
				CheckRegister(callback, preTickMethods);
				preTickMethods.Add(callback);
			}
		}

		/// <summary>
		/// Registers a callback to be invoked after each game tick.
		/// </summary>
		public static void RegisterPostTickCallback(UpdateCallback callback)
		{
			lock (postTickMethods)
			{
				CheckRegister(callback, postTickMethods);
				postTickMethods.Add(callback);
			}
		}

		/// <summary>
		/// Registers a callback to be invoked before each render frame.
		/// </summary>
		public static void RegisterPreRenderCallback(UpdateCallback callback)
		{
			lock (preRenderMethods)
			{
				CheckRegister(callback, preRenderMethods);
				preRenderMethods.Add(callback);
			}
		}

		/// <summary>
		/// Registers a callback to be invoked after each render frame.
		/// </summary>
		public static void RegisterPostRenderCallback(UpdateCallback callback)
		{
			lock (postRenderMethods)
			{
				CheckRegister(callback, postRenderMethods);
				postRenderMethods.Add(callback);
			}
		}

		/// <summary>
		/// Registers a callback to be invoked before each DynamicDrawManager.DrawDynamicThings.
		/// </summary>
		public static void RegisterPreDynamicDrawCallback(UpdateCallback callback)
		{
			lock (preDynamicDrawMethods)
			{
				CheckRegister(callback, preDynamicDrawMethods);
				preDynamicDrawMethods.Add(callback);
			}
		}

		/// <summary>
		/// Registers a callback to be invoked when caches should be cleared (game load).
		/// </summary>
		public static void RegisterClearCacheCallback(ClearCacheCallback callback)
		{
			lock (clearCacheMethods)
			{
				CheckRegister(callback, clearCacheMethods);
				clearCacheMethods.Add(callback);
			}
		}

		private static void CheckRegister<T>(T callback, List<T> registerTo) where T : Delegate
		{
			if (callback == null)
				throw new NullReferenceException("Callback is null");

			if (!registerTo.Contains(callback))
				return;

			var method = callback.Method;
			var type = method.DeclaringType;
			var typeName = type != null ? type.FullName : string.Empty;

			if (!method.IsStatic)
				throw new Exception($"Cannot register non-static method {typeName}:{method.Name}");

			YaOptMod.Error($"Attempting to register {typeName}:{method.Name} multiple times");
		}

		public static void PreRender()
		{
			YaOptGlobal.IsRendering = true;
			var tick = Find.TickManager.TicksGame;
			for (var i = 0; i < preRenderMethods.Count; i++)
			{
				var callback = preRenderMethods[i];
				try
				{
					callback(tick);
				}
				catch (Exception ex)
				{
					Log.Error(ex.ToString());
				}
			}
		}

		public static void PostRender()
		{
			YaOptGlobal.IsRendering = false;
			var tick = Find.TickManager.TicksGame;
			for (var i = 0; i < postRenderMethods.Count; i++)
			{
				var callback = postRenderMethods[i];
				try
				{
					callback(tick);
				}
				catch (Exception ex)
				{
					Log.Error(ex.ToString());
				}
			}
		}

		public static void PreTick()
		{
			var tick = Find.TickManager.TicksGame;
			for (var i = 0; i < preTickMethods.Count; i++)
			{
				var callback = preTickMethods[i];
				try
				{
					callback(tick);
				}
				catch (Exception ex)
				{
					Log.Error(ex.ToString());
				}
			}
		}

		public static void PostTick()
		{
			var tick = Find.TickManager.TicksGame;
			for (var i = 0; i < postTickMethods.Count; i++)
			{
				var callback = postTickMethods[i];
				try
				{
					callback(tick);
				}
				catch (Exception ex)
				{
					Log.Error(ex.ToString());
				}
			}
		}

		public static void PreDynamicDraw()
		{
			var tick = Find.TickManager.TicksGame;
			for (var i = 0; i < preDynamicDrawMethods.Count; i++)
			{
				var callback = preDynamicDrawMethods[i];
				try
				{
					callback(tick);
				}
				catch (Exception ex)
				{
					Log.Error(ex.ToString());
				}
			}
		}

		public static void ClearCache()
		{
			for (var i = 0; i < clearCacheMethods.Count; i++)
			{
				var callback = clearCacheMethods[i];
				try
				{
					callback();
				}
				catch (Exception ex)
				{
					Log.Error(ex.ToString());
				}
			}
		}
	}
}