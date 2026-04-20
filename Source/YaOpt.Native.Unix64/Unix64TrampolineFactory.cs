using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using YaOpt.Helpers.Trampolines;
using YaOpt.Native.Unix64.Trampolines;
using YaOpt.Patches.Trampolines;

namespace YaOpt.Native.Unix64
{
	/// <summary>
	/// Unix x64 implementation of trampoline factory for patching generic methods.
	/// See <see cref="Win64TrampolineFactory"/> for details.
	/// </summary>
	internal class Unix64TrampolineFactory : TrampolineFactory
	{
		private const int PROT_READ = 0x1;
		private const int PROT_WRITE = 0x2;
		private const int PROT_EXEC = 0x4;
		private const int PROT_READWRITEEXEC = PROT_READ | PROT_WRITE | PROT_EXEC;
		private const long PAGE_SIZE = 4096;

		[DllImport("libc", SetLastError = true)]
		private static extern int mprotect(IntPtr addr, UIntPtr len, int prot);

		public class TrampolineLinux64 : Trampoline
		{
			public TrampolineLinux64(MethodInfo sourceMethod, byte[] trampolineCode, byte[] originalMethodCode) :
				base(sourceMethod, trampolineCode, originalMethodCode)
			{
			}

			protected override void Write(byte[] codeBytes)
			{
				var srcPtr = SourceMethod.MethodHandle.GetFunctionPointer();
				var codeLength = codeBytes.Length;

				long startAddress = srcPtr.ToInt64();
				long pageStart = startAddress & ~(PAGE_SIZE - 1);
				long endAddress = startAddress + codeLength;
				long length = endAddress - pageStart;

				if (mprotect(new IntPtr(pageStart), new UIntPtr((ulong)length), PROT_READWRITEEXEC) != 0)
					throw new Exception("Cannot set memory permissions for " + SourceMethod.Name +
										" on address 0x" + srcPtr.ToString("X"));

				Marshal.Copy(codeBytes, 0, srcPtr, codeLength);

				// Restore to Read/Execute
				if (mprotect(new IntPtr(pageStart), new UIntPtr((ulong)length), PROT_READ | PROT_EXEC) != 0)
					YaOptMod.Warning("Cannot restore memory permissions for " + SourceMethod.Name +
									 " on address 0x" + srcPtr.ToString("X"));
			}
		}

		public static void CreateInstance()
		{
			if (Instance != null)
				return;
			Instance = new Unix64TrampolineFactory();
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

			return new TrampolineLinux64(srcMethod, trampolineCode, originalCode);
		}

		public override void CreateTrampolineInstallers()
		{
			Verse_ContentFinder_Get.Instance = new Verse_ContentFinder_Get_Unix64();
			Verse_ThingWithComps_GetComp.Instance = new Verse_ThingWithComps_GetComp_Unix64();
		}
	}
}