namespace YaOpt.Native.Unix64
{
	public static class Initer
	{
		public static void Init()
		{
			Unix64TrampolineFactory.CreateInstance();
		}
	}
}