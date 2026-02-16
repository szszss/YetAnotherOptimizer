using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using YaOpt.Helpers.Trampolines;
using YaOpt.Native.Win64.Trampolines;
using YaOpt.Patches.Trampolines;

namespace YaOpt.Native.Win64
{
	internal class Win64TrampolineFactory : TrampolineFactory
	{
		private const int PAGE_EXECUTE_READWRITE = 0x40;

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool VirtualProtect(IntPtr lpAddress, int dwSize, int flNewProtect, out int lpflOldProtect);

		public class TrampolineWin64 : Trampoline
		{
			public TrampolineWin64(MethodInfo sourceMethod, byte[] trampolineCode, byte[] originalMethodCode) :
				base(sourceMethod, trampolineCode, originalMethodCode)
			{
			}

			protected override void Write(byte[] codeBytes)
			{
				var srcPtr = SourceMethod.MethodHandle.GetFunctionPointer();
				var codeLength = codeBytes.Length;
				if (!VirtualProtect(srcPtr, codeLength, PAGE_EXECUTE_READWRITE, out var oldProtect))
					throw new Exception("Cannot set memory permissions for " + SourceMethod.Name +
										" on address 0x" + srcPtr.ToString("X"));

				Marshal.Copy(codeBytes, 0, srcPtr, codeLength);

				if (!VirtualProtect(srcPtr, codeLength, oldProtect, out var _))
					YaOptMod.Warning("Cannot restore memory permissions for " + SourceMethod.Name +
									 " on address 0x" + srcPtr.ToString("X"));
			}
		}

		public static void CreateInstance()
		{
			if (Instance != null)
				return;
			Instance = new Win64TrampolineFactory();
		}

		public override Trampoline CreateTrampoline(MethodInfo srcMethod, MethodInfo targetMethod, byte[] prefixCode = null)
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

			return new TrampolineWin64(srcMethod, trampolineCode, originalCode);
		}

		public override void CreateTrampolineInstallers()
		{
			Verse_ContentFinder_Get.Instance = new Verse_ContentFinder_Get_Win64();
			Verse_ThingWithComps_GetComp.Instance = new Verse_ThingWithComps_GetComp_Win64();
		}
	}
}