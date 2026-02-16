using System.Reflection;

namespace YaOpt.Helpers.Trampolines
{
	public abstract class TrampolineFactory
	{
		public static TrampolineFactory Instance { set; get; }

		public static bool IsAvailable => Instance != null;

		public abstract Trampoline CreateTrampoline(MethodInfo getCompMethod, MethodInfo targetMethod, byte[] prefixCode = null);

		public abstract void CreateTrampolineInstallers();
	}
}