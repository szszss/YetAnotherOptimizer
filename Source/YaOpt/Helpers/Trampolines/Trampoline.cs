using System.Reflection;

namespace YaOpt.Helpers.Trampolines
{
	public abstract class Trampoline
	{
		protected readonly MethodInfo SourceMethod;

		private readonly byte[] _trampolineCode;

		private readonly byte[] _originalMethodCode;

		protected Trampoline(MethodInfo sourceMethod, byte[] trampolineCode, byte[] originalMethodCode)
		{
			SourceMethod = sourceMethod;
			_trampolineCode = trampolineCode;
			_originalMethodCode = originalMethodCode;
		}

		protected abstract void Write(byte[] codeBytes);

		public void Install()
		{
			Write(_trampolineCode);
		}

		public void Uninstall()
		{
			Write(_originalMethodCode);
		}
	}
}