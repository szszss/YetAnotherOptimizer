using SharpDisasm;
using SharpDisasm.Udis86;
using System;
using System.Runtime.InteropServices;
using YaOpt.Helpers;
using YaOpt.Patches.Trampolines;

namespace YaOpt.Native.Win64.Trampolines
{
	/// <summary>
	/// Windows x64 trampoline installer for <c>ContentFinder&lt;T&gt;.Get()</c>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <b>Target Method:</b> <c>Verse.ContentFinder&lt;T&gt;.Get(string itemPath, bool reportFailure)</c>
	/// </para>
	/// <para>
	/// <b>Purpose:</b> Enables lazy texture loading by intercepting content lookups.
	/// </para>
	/// <para>
	/// <b>x64 Machine Code:</b>
	/// This trampoline generates custom x64 assembly that:
	/// <list type="number">
	/// <item>Saves original parameters and non-volatile registers to the stack.</item>
	/// <item>Retrieves the generic type from MRGCTX.</item>
	/// <item>Passes the generic type as an additional parameter to our patch method.</item>
	/// <item>Restores registers and jumps to the patch method.</item>
	/// </list>
	/// </para>
	/// <para>
	/// <b>Safety:</b> The machine code includes null checks and version tracking
	/// to ensure cache validity across component modifications.
	/// </para>
	/// </remarks>
	/// <seealso cref="Verse_ContentFinder_Get"/>
	/// <seealso cref="YaOptSettings.OptLazyTextureLoad"/>
	internal class Verse_ContentFinder_Get_Win64 : Verse_ContentFinder_Get
	{
		/// <summary>
		/// Pre-assembled x64 machine code for the trampoline prefix.
		/// </summary>
		/// <remarks>
		/// <para>
		/// This code:
		/// <list type="bullet">
		/// <item>Allocates 0x40 bytes of stack space for local storage.</item>
		/// <item>Saves parameters (rcx=itemPath, rdx=reportFailure) to stack.</item>
		/// <item>Saves non-volatile registers (r12-r15) per x64 ABI.</item>
		/// <item>Loads the generic type from MRGCTX (r10) via indirect call.</item>
		/// <item>Restores all saved values and prepares for the jump.</item>
		/// </list>
		/// </para>
		/// <para>
		/// The placeholder <c>0xFFFFFFFF</c> is replaced at runtime with the actual
		/// generic type getter address found in the source method.
		/// </para>
		/// </remarks>
		private static readonly byte[] PRECODE =
		{
			/* 
			 * push   rbp
			 * mov    rbp,rsp
			 * sub    rsp,0x40
			 * mov    QWORD PTR [rbp-0x8],rcx  // Store the itemPath to stack
			 * mov    QWORD PTR [rbp-0x10],rdx // Store the reportFailure to stack
			 * mov    QWORD PTR [rbp-0x18],r12
			 * mov    QWORD PTR [rbp-0x20],r13
			 * mov    QWORD PTR [rbp-0x28],r14
			 * mov    QWORD PTR [rbp-0x30],r15
			 * xor    r15d,r15d
			 * mov    rcx, r10
			 * movabs r11,0xFFFFFFFF           // Placeholder for Generics Getter of MRGCTX
			 * call   r11
			 * mov    rcx,rax                  // Load the generic Type into parameter 1
			 * mov    rdx,QWORD PTR [rbp-0x8]  // Load the itemPath into parameter 2
			 * mov    r8,QWORD PTR [rbp-0x10]  // Load the reportFailure into parameter 3
			 * mov    r15,QWORD PTR [rbp-0x30]
			 * mov    r14,QWORD PTR [rbp-0x28]
			 * mov    r13,QWORD PTR [rbp-0x20]
			 * mov    r12,QWORD PTR [rbp-0x18]
			 * lea    rsp,[rbp+0x0]
			 * pop    rbp
			 */
			0x55, 0x48, 0x89, 0xE5, 0x48, 0x83, 0xEC, 0x40, 0x48, 0x89, 0x4D, 0xF8, 0x48, 0x89, 0x55, 0xF0, 0x4C, 0x89,
			0x65, 0xE8, 0x4C, 0x89, 0x6D, 0xE0, 0x4C, 0x89, 0x75, 0xD8, 0x4C, 0x89, 0x7D, 0xD0, 0x45, 0x31, 0xFF, 0x4C,
			0x89, 0xD1, 0x49, 0xBB, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x41, 0xFF, 0xD3, 0x48, 0x89, 0xC1,
			0x48, 0x8B, 0x55, 0xF8, 0x4C, 0x8B, 0x45, 0xF0, 0x4C, 0x8B, 0x7D, 0xD0, 0x4C, 0x8B, 0x75, 0xD8, 0x4C, 0x8B,
			0x6D, 0xE0, 0x4C, 0x8B, 0x65, 0xE8, 0x48, 0x8D, 0x65, 0x00, 0x5D
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
		/// <item>Copy the first 0x200 bytes of the source method's machine code.</item>
		/// <item>Search for the pattern: <c>LEA RSP, [RSP+0]</c> followed by <c>MOVABS R11, address</c>.</item>
		/// <item>Extract the 8-byte address from the <c>MOVABS</c> instruction.</item>
		/// <item>Replace the placeholder in <see cref="PRECODE"/> with this address.</item>
		/// </list>
		/// </para>
		/// <para>
		/// This address is the CLR's internal function for resolving generic types at runtime,
		/// which is needed to pass the generic type parameter to our patch method.
		/// </para>
		/// </remarks>
		protected override void CreateCode()
		{
			var funcPtr = SourceMethod.MethodHandle.GetFunctionPointer();
			var codes = new byte[0x200];
			Marshal.Copy(funcPtr, codes, 0, codes.Length);
			var disasm = new Disassembler(codes, ArchitectureMode.x86_64);
			var offsetMRGCTX = 0;
			var addressGetter = 0L;
			var stage = 0;
			// find the typeof(T) of "if (typeof(T) != typeof(Shader))"
			foreach (var instruction in disasm.Disassemble())
			{
				// find the offset from "MOV  QWORD PTR [rbp-offset],r10", where r10 stores the MRGCTX
				if (stage == 0 && instruction.Mnemonic == ud_mnemonic_code.UD_Imov &&
					instruction.Operands.Length == 2 &&
					instruction.Operands[0].Type == ud_type.UD_OP_MEM &&
					instruction.Operands[0].Base == ud_type.UD_R_RBP &&
					instruction.Operands[1].Type == ud_type.UD_OP_REG &&
					instruction.Operands[1].Base == ud_type.UD_R_R10)
				{
					offsetMRGCTX = instruction.Operands[0].LvalSDWord;
					stage++;
				}
				// find "MOV  rcx,QWORD PTR [rbp-offset]", which loads the MRGCTX to rcx
				else if (stage == 1 && instruction.Mnemonic == ud_mnemonic_code.UD_Imov &&
					instruction.Operands.Length == 2 &&
					instruction.Operands[0].Type == ud_type.UD_OP_REG &&
					instruction.Operands[0].Base == ud_type.UD_R_RCX &&
					instruction.Operands[1].Type == ud_type.UD_OP_MEM &&
					instruction.Operands[1].Base == ud_type.UD_R_RBP &&
					instruction.Operands[1].LvalSDWord == offsetMRGCTX)
				{
					stage++;
				}
				// find the nearest "MOVABS  r11,address" where address is the getter
				else if (stage == 2 && instruction.Mnemonic == ud_mnemonic_code.UD_Imov &&
					instruction.Operands[0].Type == ud_type.UD_OP_REG &&
					instruction.Operands[0].Base == ud_type.UD_R_R11 &&
					instruction.Operands[1].Type == ud_type.UD_OP_IMM)
				{
					addressGetter = instruction.Operands[1].Value;
					stage++;
					break;
				}
			}

			if (stage != 3)
			{
				throw new Exception("Cannot find the address of generic type getter for ContentFinder.Get. " +
									$"Current OS:{Environment.OSVersion} " +
									$"Codes: {codes.PrintBytesInHex()}");
			}

			var genericTypeGetterAddress = BitConverter.GetBytes(addressGetter);
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