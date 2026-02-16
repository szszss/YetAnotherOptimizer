using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using YaOpt.Helpers;

namespace YaOpt.Native.Win64
{
	internal class Win64Hook : AsmHelper.ITrampolineFactory
	{
		private const int PAGE_EXECUTE_READWRITE = 0x40;

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool VirtualProtect(IntPtr lpAddress, int dwSize, int flNewProtect, out int lpflOldProtect);

		public class Trampoline : AsmHelper.ITrampoline
		{
			public MethodInfo SourceMethod;

			public byte[] TrampolineCode;

			public byte[] OriginalMethodCode;

			public Trampoline(MethodInfo sourceMethod, byte[] trampolineCode, byte[] originalMethodCode)
			{
				SourceMethod = sourceMethod;
				TrampolineCode = trampolineCode;
				OriginalMethodCode = originalMethodCode;
			}

			public void Install()
			{
				Do(true);
			}

			public void Uninstall()
			{
				Do(false);
			}

			private void Do(bool install)
			{
				var srcPtr = SourceMethod.MethodHandle.GetFunctionPointer();
				var codeLength = TrampolineCode.Length;
				if (!VirtualProtect(srcPtr, codeLength, PAGE_EXECUTE_READWRITE, out var oldProtect))
					throw new Exception("Cannot set memory permissions for " + SourceMethod.Name +
										" on address 0x" + srcPtr.ToString("X"));

				Marshal.Copy(install ? TrampolineCode : OriginalMethodCode, 0, srcPtr, codeLength);

				if (!VirtualProtect(srcPtr, codeLength, oldProtect, out var _))
					YaOptMod.Warning("Cannot restore memory permissions for " + SourceMethod.Name +
									 " on address 0x" + srcPtr.ToString("X"));
			}
		}

		public static void CreateInstance()
		{
			if (AsmHelper.TrampolineFactory != null)
				return;
			AsmHelper.TrampolineFactory = new Win64Hook();
		}

		public AsmHelper.ITrampoline CreateTrampoline(MethodInfo srcMethod, MethodInfo targetMethod, byte[] prefixCode = null)
		{
			RuntimeHelpers.PrepareMethod(targetMethod.MethodHandle);
			var srcPtr = srcMethod.MethodHandle.GetFunctionPointer();
			var targetPtr = targetMethod.MethodHandle.GetFunctionPointer();
			if (srcPtr == IntPtr.Zero)
				throw new MissingMethodException("Cannot get the function pointer of " + srcMethod.Name);
			if (targetPtr == IntPtr.Zero)
				throw new MissingMethodException("Cannot get the function pointer of " + targetMethod.Name);
			var trampolineCode = (prefixCode ?? Array.Empty<byte>())
				.Append((byte)0x48)
				.Append((byte)0xB8)
				.Concat(BitConverter.GetBytes(targetPtr.ToInt64()))
				.Append((byte)0xFF)
				.Append((byte)0xE0)
				.ToArray();
			var originalCode = new byte[trampolineCode.Length];
			Marshal.Copy(srcPtr, originalCode, 0, originalCode.Length);

			return new Trampoline(srcMethod, trampolineCode, originalCode);
		}
	}
}