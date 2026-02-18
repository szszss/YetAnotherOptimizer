using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using Verse;

namespace YaOpt.Helpers
{
	public class DebugHelper
	{
		public static void Init()
		{
#if !DEBUG
			return;
#endif
			// 1. ContentFinder<Texture2D>.Get
			var method1 = typeof(ContentFinder<Texture2D>).GetMethod("Get", new Type[] { typeof(string), typeof(bool) });
			if (method1 != null)
			{
				LogMethodBytes(method1, "ContentFinder<Texture2D>.Get");
			}
			else
			{
				YaOptMod.Warning("DebugHelper: Failed to find ContentFinder<Texture2D>.Get");
			}

			// 2. ThingWithComps.GetComp (using ThingComp as the generic argument to ensure we have a JITted instance)
			var genericMethod = typeof(ThingWithComps).GetMethod("GetComp");
			if (genericMethod != null)
			{
				var method2 = genericMethod.MakeGenericMethod(typeof(ThingComp));
				LogMethodBytes(method2, "ThingWithComps.GetComp<ThingComp>");
			}
			else
			{
				YaOptMod.Warning("DebugHelper: Failed to find ThingWithComps.GetComp");
			}
		}

		private static void LogMethodBytes(MethodInfo method, string methodName)
		{
			try
			{
				RuntimeHelpers.PrepareMethod(method.MethodHandle);
				IntPtr ptr = method.MethodHandle.GetFunctionPointer();

				YaOptMod.Warning($"DebugHelper: Reading 512 bytes from {methodName} at 0x{ptr.ToInt64():X}");

				byte[] buffer = new byte[512];
				Marshal.Copy(ptr, buffer, 0, 512);

				StringBuilder sb = new StringBuilder();
				for (int i = 0; i < buffer.Length; i++)
				{
					sb.Append(buffer[i].ToString("X2"));
					if (i < buffer.Length - 1)
					{
						sb.Append(" ");
					}
				}

				YaOptMod.Warning($"{methodName} bytes: {sb}");
			}
			catch (Exception ex)
			{
				YaOptMod.Warning($"DebugHelper: Exception reading bytes from {methodName}: {ex}");
			}
		}
	}
}
