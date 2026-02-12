using System;
using System.Reflection;
using System.Security.Cryptography;
using Unity.Jobs;
using UnityEngine;

namespace YaOpt.Helpers
{
	public static class MiscHelper
	{
		public static string DeclaringTypeName(this MethodBase method)
		{
			var type = method.DeclaringType;
			return type != null ? type.FullName : string.Empty;
		}

		public static string FullName(this MethodBase method)
		{
			return $"{method.DeclaringTypeName()}:{method.Name}";
		}

		public static Hash128 GetMethodBodyHash(MethodBase method)
		{
			var ilBytes = method.GetMethodBody()?.GetILAsByteArray();
			if (ilBytes == null)
				throw new MethodAccessException($"Cannot get the IL code of method {method.FullName()}");

			using (var hasher = SHA1.Create())
			{
				var hash = Hash128.Compute(hasher.ComputeHash(ilBytes));
				YaOptMod.Debug($"MethodBody Hash: {method.FullName()} - {hash}");
				return hash;
			}
		}

		public static bool CheckHash(MethodBase method, Hash128 hash, bool throwException = false, bool warn = true)
		{
			try
			{
				var methodHash = GetMethodBodyHash(method);
				if (methodHash == hash)
					return true;

				if (warn)
				{
					YaOptMod.Warning($"Method {method.FullName()} code does not match the expected code.\n" +
									 $"Actual code hash: {methodHash}\n" +
									 $"Expected code hash: {hash}\n" +
									 "This may cause the patch to fail. " +
									 "This issue is caused by YaOpt. Please report it to the YaOpt developers.");
				}

				if (throwException)
					throw new Exception($"Method {method.FullName()} code does not match the expected code.\n" +
										$"Actual code hash: {methodHash}\n" +
										$"Expected code hash: {hash}\n" +
										"This issue is caused by YaOpt. Please report it to the YaOpt developers.");

			}
			catch (MethodAccessException)
			{
				if (warn)
					YaOptMod.Warning($"Cannot get the IL code of method {method.FullName()}");
				if (throwException)
					throw;
			}
			return false;
		}

		public static void CompleteWithSpinWait(this JobHandle jobHandle)
		{
			if (jobHandle.Equals(default))
				return;
			while (!jobHandle.IsCompleted)
			{
			}
		}

		// Not useful. No performance improvement
		/*[StructLayout(LayoutKind.Explicit)]
		private struct SignalArgsView
		{
			[FieldOffset(0)]  public SignalArgs SingnalArgsMember;
			[FieldOffset(0)]  public int Count;
			[FieldOffset(4)]  private int padding;
			[FieldOffset(8)]  public object Arg1;
			[FieldOffset(16)] public string Label1;
			[FieldOffset(24)] public object Arg2;
			[FieldOffset(32)] public string Label2;
			[FieldOffset(40)] public object Arg3;
			[FieldOffset(48)] public string Label3;
			[FieldOffset(56)] public object Arg4;
			[FieldOffset(64)] public string Label4;
			[FieldOffset(72)] public NamedArgument[] Args;
		}

		public static bool SignalArgsTryGetArgFast<T>(in SignalArgs singnalArgs, string name, out T arg) where T : class
		{
			var signalArgsView = new SignalArgsView() {SingnalArgsMember = singnalArgs};
			if (signalArgsView.Count == 0)
			{
			}
			else if (signalArgsView.Args != null)
			{
				for (var i = 0; i < signalArgsView.Args.Length; i++)
				{
					var namedArgument = signalArgsView.Args[i];
					if (namedArgument.label == name && namedArgument.arg is T t)
					{
						arg = t;
						return true;
					}
				}
			}
			else
			{
				if (signalArgsView.Count >= 1 && signalArgsView.Label1 == name && signalArgsView.Arg1 is T t1)
				{
					arg = t1;
					return true;
				}
				if (signalArgsView.Count >= 2 && signalArgsView.Label2 == name && signalArgsView.Arg2 is T t2)
				{
					arg = t2;
					return true;
				}
				if (signalArgsView.Count >= 3 && signalArgsView.Label3 == name && signalArgsView.Arg3 is T t3)
				{
					arg = t3;
					return true;
				}
				if (signalArgsView.Count >= 4 && signalArgsView.Label4 == name && signalArgsView.Arg4 is T t4)
				{
					arg = t4;
					return true;
				}
			}
			arg = null;
			return false;
		}*/
	}
}