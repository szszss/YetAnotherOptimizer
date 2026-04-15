using System;
using System.Runtime.InteropServices;

namespace YaOpt.Native.Win64.Trampolines
{
	/// <summary>
	/// Windows x64 trampoline installer for <c>ThingWithComps.GetComp&lt;T&gt;()</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <b>Target Method:</b> <c>Verse.ThingWithComps.GetComp&lt;T&gt;()</c>
	/// </para>
	/// <para>
	/// <b>Purpose:</b> Optimizes component lookup by caching the result for fast failure
	/// when a component doesn't exist, avoiding expensive list iteration.
	/// </para>
	/// <para>
	/// <b>x64 Machine Code:</b>
	/// This trampoline generates custom x64 assembly that:
	/// <list type="number">
	/// <item>Performs an early null check on the comps list (fast path).</item>
	/// <item>Captures the comps list version for change detection.</item>
	/// <item>Retrieves the generic type from MRGCTX.</item>
	/// <item>Passes additional context (comps version, compsByType cache) to the arguments.</item>
	/// <item>Restores registers and jumps to the patch method.</item>
	/// </list>
	/// </para>
	/// <para>
	/// <b>Safety:</b> The machine code includes null checks and version tracking
	/// to ensure cache validity across component modifications.
	/// </para>
	/// </remarks>
	/// <seealso cref="Verse_ThingWithComps_GetComp"/>
	/// <seealso cref="YaOptSettings.OptThingGetComp"/>
	internal class Verse_ThingWithComps_GetComp_Win64 : Verse_ThingWithComps_GetComp
	{
		/// <summary>
		/// Pre-assembled x64 machine code for the trampoline prefix.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This code:
		/// <list type="bullet">
		/// <item>Early-exits if <c>this.comps</c> is null.</item>
		/// <item>Allocates 0x50 bytes of stack space.</item>
		/// <item>Saves all non-volatile registers per x64 ABI.</item>
		/// <item>Captures the comps list version (offset 0x1C in List&lt;T&gt;).</item>
		/// <item>Loads the generic type from MRGCTX (r10).</item>
		/// <item>Passes: this, generic type, list version, compsByType cache.</item>
		/// </list>
		/// </para>
		/// <para>
		/// The placeholder <c>0xFFFFFFFFFFFFFFFF</c> is replaced at runtime with the
		/// actual generic type getter address.
		/// </para>
		/// </remarks>
		private static readonly byte[] PRECODE =
		{
			/* 
			 * mov    rax,QWORD PTR [rcx+0xb8]
			 * test   rax,rax
			 * jnz    not_null
			 * ret	// Return if comps is null
			 * nop	// Align
			 * nop
			 * nop
			 * not_null:
			 * push	rbp
			 * mov	rbp,rsp
			 * sub	rsp,0x50
			 * mov	QWORD PTR [rbp-0x8],rbx
			 * mov	QWORD PTR [rbp-0x10],rsi
			 * mov	QWORD PTR [rbp-0x18],rdi
			 * mov	QWORD PTR [rbp-0x20],r12
			 * mov	QWORD PTR [rbp-0x28],r13
			 * mov	QWORD PTR [rbp-0x30],r14
			 * mov	QWORD PTR [rbp-0x38],r15
			 * mov	QWORD PTR [rbp-0x40],r10
			 * mov	rsi,rcx
			 * mov	r12,QWORD PTR [rsi+0xb8]	// Read the version of comps
			 * mov	eax,DWORD PTR [r12+0x1c]	// 0x1C is the offset of _version field of List
			 * mov	QWORD PTR [rbp-0x48],rax
			 * mov	rcx, r10
			 * movabs	r11, 0xFFFFFFFFFFFFFFFF	// Placeholder for Generics Getter of MRGCTX
			 * call	r11							// Read the generic Type from MRGCTX
			 * mov	rcx, rsi					// Load the ThingWithComp instance into parameter 1
			 * mov	rdx, rax					// Load the generic Type into parameter 2
			 * mov	r8, QWORD PTR [rbp-0x48]	// Load the version of comps list into parameter 3
			 * mov	r9, QWORD PTR [rsi+0xc0]	// Load compsByType into parameter 4
			 * xor	r15d,r15d
			 * xor	eax,eax
			 * mov	rbx,QWORD PTR [rbp-0x8]
			 * mov	rsi,QWORD PTR [rbp-0x10]
			 * mov	rdi,QWORD PTR [rbp-0x18]
			 * mov	r12,QWORD PTR [rbp-0x20]
			 * mov	r13,QWORD PTR [rbp-0x28]
			 * mov	r14,QWORD PTR [rbp-0x30]
			 * mov	r15,QWORD PTR [rbp-0x38]
			 * mov	r10,QWORD PTR [rbp-0x40]
			 * lea	rsp,[rbp+0x0]
			 * pop	rbp
			 */
			0x48, 0x8B, 0x81, 0xB8, 0x00, 0x00, 0x00, 0x48, 0x85, 0xC0, 0x75, 0x04, 0xC3, 0x90, 0x90, 0x90, 0x55, 0x48,
			0x89, 0xE5, 0x48, 0x83, 0xEC, 0x50, 0x48, 0x89, 0x5D, 0xF8, 0x48, 0x89, 0x75, 0xF0, 0x48, 0x89, 0x7D, 0xE8,
			0x4C, 0x89, 0x65, 0xE0, 0x4C, 0x89, 0x6D, 0xD8, 0x4C, 0x89, 0x75, 0xD0, 0x4C, 0x89, 0x7D, 0xC8, 0x4C, 0x89,
			0x55, 0xC0, 0x48, 0x89, 0xCE, 0x4C, 0x8B, 0xA6, 0xB8, 0x00, 0x00, 0x00, 0x41, 0x8B, 0x44, 0x24, 0x1C, 0x48,
			0x89, 0x45, 0xB8, 0x4C, 0x89, 0xD1, 0x49, 0xBB, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x41, 0xFF,
			0xD3, 0x48, 0x89, 0xF1, 0x48, 0x89, 0xC2, 0x4C, 0x8B, 0x45, 0xB8, 0x4C, 0x8B, 0x8E, 0xC0, 0x00, 0x00, 0x00,
			0x45, 0x31, 0xFF, 0x31, 0xC0, 0x48, 0x8B, 0x5D, 0xF8, 0x48, 0x8B, 0x75, 0xF0, 0x48, 0x8B, 0x7D, 0xE8, 0x4C,
			0x8B, 0x65, 0xE0, 0x4C, 0x8B, 0x6D, 0xD8, 0x4C, 0x8B, 0x75, 0xD0, 0x4C, 0x8B, 0x7D, 0xC8, 0x4C, 0x8B, 0x55,
			0xC0, 0x48, 0x8D, 0x65, 0x00, 0x5D
		};

		/// <summary>
		/// Creates the trampoline prefix code by extracting the generic type getter address.
		/// </summary>
		/// <exception cref="Exception">
		/// Thrown when the generic type getter address cannot be found in the source method.
		/// </exception>
		/// <remarks>
		/// <para>
		/// <b>Process:</b>
		/// <list type="number">
		/// <item>Copy the first 0x400 bytes of the source method's machine code.</item>
		/// <item>Search for the <c>compsByType</c> field access pattern (offset 0xC0) - need to find it twice.</item>
		/// <item>After the second occurrence, find the <c>MOVABS R11, address</c> instruction.</item>
		/// <item>Extract the 8-byte address and replace the placeholder in <see cref="PRECODE"/>.</item>
		/// </list>
		/// </para>
		/// <para>
		/// The "find twice" logic ensures we skip the null-check access and find the actual usage point.
		/// </para>
		/// </remarks>
		protected override void CreateCode()
		{
			var funcPtr = SourceMethod.MethodHandle.GetFunctionPointer();
			var codes = new byte[0x400];
			var genericTypeGetterAddress = new byte[8];
			Marshal.Copy(funcPtr, codes, 0, codes.Length);
			var foundCompsByType = 0;
			var foundAddress = false;
			for (int i = 0, j = codes.Length - 10; i < j; i++)
			{
				// Search 0xC0 0x00 0x00 0x00, which is the address of compsByType
				// First one is null check. We need the second one.
				if (foundCompsByType < 2 && codes[i] == 0xC0 && codes[i + 1] == 0 && codes[i + 2] == 0 && codes[i + 3] == 0)
				{
					foundCompsByType++;
				}
				// Search 0x49 0xBB, which is movabs r11
				if (foundCompsByType > 1 && codes[i] == 0x49 && codes[i + 1] == 0xBB)
				{
					Array.Copy(codes, i + 2, genericTypeGetterAddress, 0, 8);
					foundAddress = true;
					break;
				}
			}
			if (!foundAddress)
				throw new Exception($"Cannot find the address of generic type getter. Current OS:{Environment.OSVersion}");
			codes = new byte[PRECODE.Length];
			PRECODE.CopyTo(codes, 0);
			for (int i = 0, j = codes.Length - 8; i < j; i++)
			{
				if (codes[i] == 0xFF && codes[i + 1] == 0xFF && codes[i + 2] == 0xFF && codes[i + 3] == 0xFF)
				{
					Array.Copy(genericTypeGetterAddress, 0, codes, i, 8);
					break;
				}
			}
			PrefixCode = codes;
		}
	}
}