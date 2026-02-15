namespace YaOpt.Native.Win64
{
	public static class Initer
	{
		public static void Init()
		{
			Win64Hook.CreateInstance();
		}
	}
}