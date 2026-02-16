using System;
using System.Runtime.InteropServices;
using YaOpt.Patches.Trampolines;

namespace YaOpt.Native.Win64.Trampolines
{
	internal class Verse_ContentFinder_Get_Win64 : Verse_ContentFinder_Get
	{
		/* Tip: You can always ask AI
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
		private static readonly byte[] PRECODE =
		{
			0x55, 0x48, 0x89, 0xE5, 0x48, 0x83, 0xEC, 0x40, 0x48, 0x89, 0x4D, 0xF8, 0x48, 0x89, 0x55, 0xF0, 0x4C, 0x89,
			0x65, 0xE8, 0x4C, 0x89, 0x6D, 0xE0, 0x4C, 0x89, 0x75, 0xD8, 0x4C, 0x89, 0x7D, 0xD0, 0x45, 0x31, 0xFF, 0x4C,
			0x89, 0xD1, 0x49, 0xBB, 0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x41, 0xFF, 0xD3, 0x48, 0x89, 0xC1,
			0x48, 0x8B, 0x55, 0xF8, 0x4C, 0x8B, 0x45, 0xF0, 0x4C, 0x8B, 0x7D, 0xD0, 0x4C, 0x8B, 0x75, 0xD8, 0x4C, 0x8B,
			0x6D, 0xE0, 0x4C, 0x8B, 0x65, 0xE8, 0x48, 0x8D, 0x65, 0x00, 0x5D
		};

		protected override void CreateCode()
		{
			var funcPtr = SourceMethod.MethodHandle.GetFunctionPointer();
			var codes = new byte[0x200];
			var genericTypeGetterAddress = new byte[8];
			Marshal.Copy(funcPtr, codes, 0, codes.Length);
			var foundLea = false;
			var foundAddress = false;
			for (int i = 0, j = codes.Length - 10; i < j; i++)
			{
				// Search 0x48 0x8D 0x64 0x24 0x00
				if (codes[i] == 0x48 && codes[i + 1] == 0x8D &&
					codes[i + 2] == 0x64 && codes[i + 3] == 0x24 && codes[i + 4] == 0)
				{
					foundLea = true;
				}
				// Search 0x49 0xBB, which is movabs r11
				if (foundLea && codes[i] == 0x49 && codes[i + 1] == 0xBB)
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