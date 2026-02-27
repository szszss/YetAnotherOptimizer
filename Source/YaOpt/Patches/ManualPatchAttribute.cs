using System;

namespace YaOpt.Patches
{
	/// <summary>
	/// The type must implement a static method named <c>Patch</c>,
	/// which will be called when the Harmony is running.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	public class ManualPatchAttribute : Attribute
	{
		/*
		 * The method should be this:
		 * static void Patch(Harmony harmony)
		 * {
		 *   if (!wantsToRun)
		 *     return;
		 *   DoPatch();
		 * }
		 *
		 * Or this:
		 * static bool Patch(Harmony harmony)
		 * {
		 *   if (!wantsToRun)
		 *     return true;
		 *   var noError = true;
		 *   foreach (var target in targetList)
		 *   {
		 *     try {
		 *       DoPatch(target);
		 *     } catch(Exception ex) {
		 *       Log(ex);
		 *       noError = false;
		 *     }
		 *   }
		 *   return noError;
		 * }
		 */
	}
}