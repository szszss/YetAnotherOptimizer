using System;

namespace YaOpt.Patches.Early
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	public class EarlyPatchAttribute : Attribute
	{
	}
}