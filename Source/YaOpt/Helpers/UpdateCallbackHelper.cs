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
			var tick = Find.TickManager.TicksGame;
			foreach (var callback in preRenderMethods)
			{
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
			var tick = Find.TickManager.TicksGame;
			foreach (var callback in postRenderMethods)
			{
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
			foreach (var callback in preTickMethods)
			{
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
			foreach (var callback in postTickMethods)
			{
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
			foreach (var callback in preDynamicDrawMethods)
			{
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
			foreach (var callback in clearCacheMethods)
			{
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