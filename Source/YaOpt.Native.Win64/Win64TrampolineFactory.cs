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
	/// <summary>
	/// Windows x64 implementation of trampoline factory for patching generic methods.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Harmony cannot directly patch generic methods due to how the runtime handles generic method instantiation.
	/// This factory works around that limitation by manually writing x64 machine code to redirect method calls.
	/// </para>
	/// <para>
	/// <b>How it works:</b>
	/// <list type="number">
	/// <item>Get the function pointer of the JITed target method.</item>
	/// <item>Modify the protection attributes of the method memory region.</item>
	/// <item>Write x64 jump instructions: <c>MOV RAX, target; JMP RAX</c>.</item>
	/// <item>The CPU executes the jump, redirecting to our patch method.</item>
	/// </list>
	/// </para>
	/// <para>
	/// <b>Safety:</b>
	/// <list type="bullet">
	/// <item>Uses <c>VirtualProtect</c> to temporarily enable write/execute permissions.</item>
	/// <item>Restores original permissions after writing.</item>
	/// <item>Stores original code for potential uninstallation.</item>
	/// </list>
	/// </para>
	/// <para>
	/// <b>Platform:</b> Only works on Windows x64.
	/// </para>
	/// </remarks>
	/// <seealso cref="TrampolineFactory"/>
	/// <seealso cref="Trampoline"/>
	internal class Win64TrampolineFactory : TrampolineFactory
	{
		/// <summary>
		/// Windows memory protection constant for read/write/execute access.
		/// </summary>
		private const int PAGE_EXECUTE_READWRITE = 0x40;

		/// <summary>
		/// Changes the memory protection of a region of committed pages.
		/// </summary>
		/// <param name="lpAddress">The starting address of the region.</param>
		/// <param name="dwSize">The size of the region in bytes.</param>
		/// <param name="flNewProtect">The new memory protection.</param>
		/// <param name="lpflOldProtect">Receives the previous memory protection.</param>
		/// <returns><c>true</c> if successful; otherwise, <c>false</c>.</returns>
		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool VirtualProtect(IntPtr lpAddress, int dwSize, int flNewProtect, out int lpflOldProtect);

		/// <summary>
		/// Windows x64 implementation of a trampoline.
		/// </summary>
		/// <remarks>
		/// Uses Windows API to modify memory permissions and write jump instructions.
		/// </remarks>
		public class TrampolineWin64 : Trampoline
		{
			/// <summary>
			/// Initializes a new instance of the <see cref="TrampolineWin64"/> class.
			/// </summary>
			/// <param name="sourceMethod">The method to be redirected.</param>
			/// <param name="trampolineCode">The x64 machine code to write (jump instructions).</param>
			/// <param name="originalMethodCode">Backup of the original code for uninstallation.</param>
			public TrampolineWin64(MethodInfo sourceMethod, byte[] trampolineCode, byte[] originalMethodCode) :
				base(sourceMethod, trampolineCode, originalMethodCode)
			{
			}

			/// <summary>
			/// Writes machine code to the source method's entry point.
			/// </summary>
			/// <param name="codeBytes">The x64 machine code to write.</param>
			/// <exception cref="Exception">
			/// Thrown when <c>VirtualProtect</c> fails to change memory permissions.
			/// </exception>
			/// <remarks>
			/// <para>
			/// <b>Process:</b>
			/// <list type="number">
			/// <item>Get the function pointer of <see cref="Trampoline.SourceMethod"/>.</item>
			/// <item>Call <c>VirtualProtect</c> to enable write access.</item>
			/// <item>Copy the code bytes using <see cref="Marshal.Copy"/>.</item>
			/// <item>Restore original memory protection.</item>
			/// </list>
			/// </para>
			/// <para>
			/// <b>Safety Warning:</b> This modifies executable memory. Incorrect code can crash the game or
			/// cause undefined behavior. Ensure code is valid x64 machine code before calling.
			/// </para>
			/// </remarks>
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

		/// <summary>
		/// Creates the singleton instance of the Windows x64 trampoline factory.
		/// </summary>
		public static void CreateInstance()
		{
			if (Instance != null)
				return;
			Instance = new Win64TrampolineFactory();
		}

		/// <summary>
		/// Creates a trampoline that redirects a source method to a target method.
		/// </summary>
		/// <param name="srcMethod">The method to be patched.</param>
		/// <param name="targetMethod">The method to redirect to.</param>
		/// <param name="prefixCode">Optional prefix bytes to prepend to the jump code.</param>
		/// <returns>A <see cref="TrampolineWin64"/> instance that can be installed or uninstalled.</returns>
		/// <exception cref="MissingMethodException">
		/// Thrown when function pointers cannot be obtained for either method.
		/// </exception>
		/// <remarks>
		/// <para>
		/// <b>x64 Jump Code:</b>
		/// <list type="bullet">
		/// <item><c>0x48 0xB8 [8-byte address]</c> - MOV RAX, target</item>
		/// <item><c>0xFF 0xE0</c> - JMP RAX</item>
		/// </list>
		/// Total: 12 bytes (or more if prefix code is provided).
		/// </para>
		/// <para>
		/// <b>Safety:</b> The source method must have at least 12 bytes of space at its entry point
		/// for the jump code. Most methods meet this requirement, but very short methods may fail.
		/// </para>
		/// </remarks>
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

		/// <summary>
		/// Creates and registers all trampoline installers for known generic methods.
		/// </summary>
		public override void CreateTrampolineInstallers()
		{
			Verse_ContentFinder_Get.Instance = new Verse_ContentFinder_Get_Win64();
			Verse_ThingWithComps_GetComp.Instance = new Verse_ThingWithComps_GetComp_Win64();
		}
	}
}