using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using YaOpt.Helpers;

namespace YaOpt.Win64
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
		    //var codeLength = 5;
		    //if (prefixCode != null)
			//    codeLength += prefixCode.Length;
		    //RuntimeHelpers.PrepareMethod(srcMethod.MethodHandle);
		    RuntimeHelpers.PrepareMethod(targetMethod.MethodHandle);
		    var srcPtr = srcMethod.MethodHandle.GetFunctionPointer();
		    var targetPtr = targetMethod.MethodHandle.GetFunctionPointer();
		    if (srcPtr == IntPtr.Zero)
			    throw new MissingMethodException("Cannot get the function pointer of " + srcMethod.Name);
		    if (targetPtr == IntPtr.Zero)
			    throw new MissingMethodException("Cannot get the function pointer of " + targetMethod.Name);
		    //var offset = targetPtr.ToInt64() - srcPtr.ToInt64() - codeLength;
		    //if (offset > int.MaxValue || offset < int.MinValue)
			//    throw new Exception(
			//	    $"Offset between the function pointer of {srcMethod.Name} (0x{srcPtr.ToString("X")}) " +
			//	    $"and {targetMethod.Name} (0x{targetPtr.ToString("X")}) is greater than 4GB ({offset})");
			// movabs rax, targetAddr
			// jmp rax
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

	    public unsafe IntPtr GetObjectMemoryAddress(object obj)
	    {
		    object* pointer = &obj;
		    return Marshal.ReadIntPtr(new IntPtr(pointer));
	    }

	    public unsafe T GetObjectFromPtr<T>(IntPtr ptr) where T : class
	    {
		    T* pT = (T*)(&ptr);
		    return *pT;
	    }
    }
}