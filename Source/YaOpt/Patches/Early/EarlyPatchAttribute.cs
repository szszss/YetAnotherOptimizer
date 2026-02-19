using System;

namespace YaOpt.Patches.Early
{
	/// <summary>
	/// Marks patches that must be applied before other mods load.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	public class EarlyPatchAttribute : Attribute
	{
	}
}