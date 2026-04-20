using SharpDisasm;
using SharpDisasm.Udis86;
using System;
using System.Runtime.InteropServices;
using YaOpt.Helpers;
using YaOpt.Patches.Trampolines;

namespace YaOpt.Native.Unix64.Trampolines
{
	/// <summary>
	/// Unix x64 trampoline installer for <c>ContentFinder&lt;T&gt;.Get()</c>.
	/// </summary>
	/// <seealso cref="Verse_ContentFinder_Get_Win64"/>
	internal class Verse_ContentFinder_Get_Unix64 : Verse_ContentFinder_Get
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
			 * sub    rsp,0x48                 // Stack alignment for System V AMD64 ABI calling convention
			 * mov    QWORD PTR [rsp+0x8],rdi  // Store the itemPath to stack
			 * mov    QWORD PTR [rsp+0x10],rsi // Store the reportFailure to stack
			 * mov    QWORD PTR [rsp+0x18],rbx
			 * mov    QWORD PTR [rsp+0x20],r12
			 * mov    QWORD PTR [rsp+0x28],r13
			 * mov    QWORD PTR [rsp+0x30],r14
			 * mov    QWORD PTR [rsp+0x38],r15
			 * xor    r13d,r13d
			 * mov    rdi, r10                 // MRGCTX to RDI
			 * movabs r11,0xFFFFFFFFFFFFFFFF   // Placeholder for Generics Getter of MRGCTX
			 * call   r11
			 * mov    rdi,rax                  // Load the generic Type into parameter 1
			 * mov    rsi,QWORD PTR [rsp+0x8]  // Load the itemPath into parameter 2
			 * mov    rdx,QWORD PTR [rsp+0x10] // Load the reportFailure into parameter 3
			 * mov    r15,QWORD PTR [rsp+0x38]
			 * mov    r14,QWORD PTR [rsp+0x30]
			 * mov    r13,QWORD PTR [rsp+0x28]
			 * mov    r12,QWORD PTR [rsp+0x20]
			 * mov    rbx,QWORD PTR [rsp+0x18]
			 * add    rsp,0x48
			 */
			0x48, 0x83, 0xEC, 0x48, 0x48, 0x89, 0x7C, 0x24, 0x08, 0x48, 0x89, 0x74, 0x24, 0x10, 0x48, 0x89, 0x5C, 0x24,
			0x18, 0x4C, 0x89, 0x64, 0x24, 0x20, 0x4C, 0x89, 0x6C, 0x24, 0x28, 0x4C, 0x89, 0x74, 0x24, 0x30, 0x4C, 0x89,
			0x7C, 0x24, 0x38, 0x45, 0x31, 0xED, 0x4C, 0x89, 0xD7, 0x49, 0xBB, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
			0xFF, 0x41, 0xFF, 0xD3, 0x48, 0x89, 0xC7, 0x48, 0x8B, 0x74, 0x24, 0x08, 0x48, 0x8B, 0x54, 0x24, 0x10, 0x4C,
			0x8B, 0x7C, 0x24, 0x38, 0x4C, 0x8B, 0x74, 0x24, 0x30, 0x4C, 0x8B, 0x6C, 0x24, 0x28, 0x4C, 0x8B, 0x64, 0x24,
			0x20, 0x48, 0x8B, 0x5C, 0x24, 0x18, 0x48, 0x83, 0xC4, 0x48
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
			var codes = new byte[0x200];
			Marshal.Copy(funcPtr, codes, 0, codes.Length);
			var disasm = new Disassembler(codes, ArchitectureMode.x86_64);
			var offsetMRGCTX = 0;
			var addressGetter = 0L;
			var stage = 0;
			// find the typeof(T) of "if (typeof(T) != typeof(Shader))"
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
				// find "MOV  rdi,QWORD PTR [rsp+offset]", which loads the MRGCTX to rdi
				else if (stage == 1 && instruction.Mnemonic == ud_mnemonic_code.UD_Imov &&
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
				else if (stage == 2 && instruction.Mnemonic == ud_mnemonic_code.UD_Icall &&
					instruction.Operands[0].Type == ud_type.UD_OP_JIMM) // you wasted my day, jim
				{
					var offset = instruction.Operands[0].LvalSDWord;
					addressGetter = (long)instruction.Offset + 5 + offset + funcPtr.ToInt64();
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