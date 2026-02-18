using System;

namespace YaOpt.Settings
{
	[Flags]
	public enum OptimizationFlags : ushort
	{
		None = 0,
		MultiplayerIncompatible = 0b0000_0001,
		RequireWin64            = 0b0000_0010,
		RequireBurst            = 0b0001_0000,

		NoSnapshot            = 0b0001_0000_0000_0000,
		IgnoreEnableAll       = 0b0010_0000_0000_0000,
		IgnoreDisableAll      = 0b0100_0000_0000_0000,
		DontSave              = 0b1000_0000_0000_0000,
	}
}