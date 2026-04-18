using SharpDisasm;
using SharpDisasm.Udis86;
using System;
using System.Runtime.InteropServices;
using YaOpt.Helpers;
using YaOpt.Patches.Trampolines;

namespace YaOpt.Native.Unix64.Trampolines
{
	/// <summary>
	/// Unix x64 trampoline installer for <c>ThingWithComps.GetComp&lt;T&gt;()</c>.
	/// </summary>
	/// <seealso cref="Verse_ThingWithComps_GetComp_Win64"/>
	internal class Verse_ThingWithComps_GetComp_Unix64 : Verse_ThingWithComps_GetComp
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
		protected override void CreateCode()
		{
			var funcPtr = SourceMethod.MethodHandle.GetFunctionPointer();
			var codes = new byte[0x400];
			Marshal.Copy(funcPtr, codes, 0, codes.Length);
			const int offsetCompsByType = 0xC0;
			var disasm = new Disassembler(codes, ArchitectureMode.x86_64);
			var offsetMRGCTX = 0;
			var addressGetter = 0L;
			var stage = 0;
			// find the typeof(T) of "if (this.compsByType.TryGetValue(typeof(T), out array))"
			foreach (var instruction in disasm.Disassemble())
			{
				// find the offset from "MOV  QWORD PTR [rsp+offset],r10", where r10 stores the MRGCTX
				if (stage == 0 && instruction.Mnemonic == ud_mnemonic_code.UD_Imov &&
				    instruction.Operands.Length == 2 &&
				    instruction.Operands[0].Type == ud_type.UD_OP_MEM &&
				    instruction.Operands[0].Base == ud_type.UD_R_RSP &&
				    instruction.Operands[1].Type == ud_type.UD_OP_REG &&
				    instruction.Operands[1].Base == ud_type.UD_R_R10)
				{
					offsetMRGCTX = instruction.Operands[0].LvalSDWord;
					stage++;
				}
				// find "MOV  rax,QWORD PTR [r15+offsetCompsByType]"
				else if (stage == 1 && instruction.Mnemonic == ud_mnemonic_code.UD_Imov &&
				         instruction.Operands.Length == 2 &&
				         instruction.Operands[0].Type == ud_type.UD_OP_REG &&
				         instruction.Operands[0].Base == ud_type.UD_R_RAX &&
				         instruction.Operands[1].Type == ud_type.UD_OP_MEM &&
				         instruction.Operands[1].Base == ud_type.UD_R_R15 &&
				         instruction.Operands[1].LvalSDWord == offsetCompsByType)
				{
					stage++;
				}
				// find "MOV  rdi,QWORD PTR [rsp+offset]", which loads the MRGCTX to rcx
				else if (stage == 2 && instruction.Mnemonic == ud_mnemonic_code.UD_Imov &&
				         instruction.Operands.Length == 2 &&
				         instruction.Operands[0].Type == ud_type.UD_OP_REG &&
				         instruction.Operands[0].Base == ud_type.UD_R_RDI &&
				         instruction.Operands[1].Type == ud_type.UD_OP_MEM &&
				         instruction.Operands[1].Base == ud_type.UD_R_RSP &&
				         instruction.Operands[1].LvalSDWord == offsetMRGCTX)
				{
					stage++;
				}
				// find the nearest "CALL  address" where address is the getter
				else if (stage == 3 && instruction.Mnemonic == ud_mnemonic_code.UD_Icall &&
				         instruction.Operands[0].Type == ud_type.UD_OP_IMM)
				{
					addressGetter = instruction.Operands[0].Value;
					stage++;
					break;
				}
			}

			if (stage != 4)
			{
				throw new Exception("Cannot find the address of generic type getter for ThingWithComps.GetComp. " +
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