using System;
using System.Collections.Generic;
using Verse;
// ReSharper disable InconsistentlySynchronizedField

namespace YaOpt.Helpers
{
	public static class UpdateCallbackHelper
	{
		public delegate void UpdateCallback(int tick);

		public delegate void ClearCacheCallback();

		private static readonly List<UpdateCallback> preTickMethods = new List<UpdateCallback>();

		private static readonly List<UpdateCallback> postTickMethods = new List<UpdateCallback>();

		private static readonly List<UpdateCallback> preRenderMethods = new List<UpdateCallback>();

		private static readonly List<UpdateCallback> postRenderMethods = new List<UpdateCallback>();

		private static readonly List<ClearCacheCallback> clearCacheMethods = new List<ClearCacheCallback>();

		public static void RegisterPreTickCallback(UpdateCallback callback)
		{
			lock (preTickMethods)
			{
				CheckRegister(callback, preTickMethods);
				preTickMethods.Add(callback);
			}
		}

		public static void RegisterPostTickCallback(UpdateCallback callback)
		{
			lock (postTickMethods)
			{
				CheckRegister(callback, postTickMethods);
				postTickMethods.Add(callback);
			}
		}

		public static void RegisterPreRenderCallback(UpdateCallback callback)
		{
			lock (preRenderMethods)
			{
				CheckRegister(callback, preRenderMethods);
				preRenderMethods.Add(callback);
			}
		}

		public static void RegisterPostRenderCallback(UpdateCallback callback)
		{
			lock (postRenderMethods)
			{
				CheckRegister(callback, postRenderMethods);
				postRenderMethods.Add(callback);
			}
		}

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