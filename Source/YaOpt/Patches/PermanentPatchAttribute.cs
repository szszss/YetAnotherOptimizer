using System;

namespace YaOpt.Patches
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	public class PermanentPatchAttribute : Attribute
	{
	}
}