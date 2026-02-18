using System;

namespace YaOpt.Settings
{
	[Flags]
	public enum OptimizationCategory : byte
	{
		Hidden = 0,
		Main = 0b0001,
		Fps  = 0b0010,
		Tps  = 0b0100,
		Misc = 0b1000,
		Any  = 0b1111
	}
}