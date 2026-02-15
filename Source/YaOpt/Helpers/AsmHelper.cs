using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace YaOpt.Helpers
{
	public static class AsmHelper
	{
		public interface ITrampolineFactory
		{
			ITrampoline CreateTrampoline(MethodInfo getCompMethod, MethodInfo targetMethod, byte[] prefixCode = null);

			IntPtr GetObjectMemoryAddress(object obj);

			T GetObjectFromPtr<T>(IntPtr ptr) where T : class;
		}

		public interface ITrampoline
		{
			void Install();

			void Uninstall();
		}

		public static ITrampolineFactory TrampolineFactory { set; get; }

		private static readonly byte[] _expectHeader =
		{
			0x55,					 //	push   rbp
			0x48, 0x8B, 0xEC,		//	mov    rbp,rsp
			0x48, 0x81, 0xEC, 0x80, 0x00, 0x00, 0x00, //	sub    rsp,0x80
			0x48, 0x89, 0x5D, 0xC8, //	mov    QWORD PTR [rbp-0x38],rbx
			0x48, 0x89, 0x75, 0xD0, //	mov    QWORD PTR [rbp-0x30],rsi
			0x48, 0x89, 0x7D, 0xD8, //	mov    QWORD PTR [rbp-0x28],rdi
			0x4C, 0x89, 0x65, 0xE0, //	mov    QWORD PTR [rbp-0x20],r12
			0x4C, 0x89, 0x6D, 0xE8, //	mov    QWORD PTR [rbp-0x18],r13
			0x4C, 0x89, 0x75, 0xF0, //	mov    QWORD PTR [rbp-0x10],r14
			0x4C, 0x89, 0x7D, 0xF8, //	mov    QWORD PTR [rbp-0x8],r15
			0x4C, 0x89, 0x55, 0xC0, //	mov    QWORD PTR [rbp-0x40],r10
			0x48, 0x8b, 0xF1		//	mov    rsi,rcx
		};

		private static readonly byte[] _search =
		{
			0x48, 0x89, 0x45, 0xA8,	// mov    QWORD PTR [rbp-0x58],rax
			0x48, 0x8B, 0x4D, 0xC0,	// mov    rcx,QWORD PTR [rbp-0x40]
			0x66, 0x66, 0x90,		// data16 xchg ax,ax
			0x49, 0xBB				// movabs
		};

		public static bool IsAvailable => TrampolineFactory != null;

		public static void CreateGetThingCompHook(MethodInfo getCompMethod, MethodInfo targetMethod)
		{
			RuntimeHelpers.PrepareMethod(getCompMethod.MethodHandle);
			RuntimeHelpers.PrepareMethod(targetMethod.MethodHandle);
			var srcPtr = getCompMethod.MethodHandle.GetFunctionPointer();
			var targetPtr = targetMethod.MethodHandle.GetFunctionPointer();
			if (srcPtr == IntPtr.Zero)
				throw new MissingMethodException("Cannot get the function pointer of " + getCompMethod.Name);
			if (targetPtr == IntPtr.Zero)
				throw new MissingMethodException("Cannot get the function pointer of " + getCompMethod.Name);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void ReadByte(BinaryReader reader, byte expect)
		{
			if (reader.ReadByte() != expect)
				throw new NotSupportedException();
		}

		// From https://stackoverflow.com/questions/2345304/
		public static IEnumerable<T> AfterSequence<T>(this IEnumerable<T> source, T[] sequence)
		{
			bool sequenceFound = false;
			Queue<T> currentSequence = new Queue<T>(sequence.Length);
			foreach (T item in source)
			{
				if (sequenceFound)
				{
					yield return item;
				}
				else
				{
					currentSequence.Enqueue(item);

					if (currentSequence.Count < sequence.Length)
						continue;

					if (currentSequence.Count > sequence.Length)
						currentSequence.Dequeue();

					if (currentSequence.SequenceEqual(sequence))
						sequenceFound = true;
				}
			}
		}
	}
}