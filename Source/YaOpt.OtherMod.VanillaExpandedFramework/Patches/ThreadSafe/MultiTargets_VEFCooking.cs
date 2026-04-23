using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using YaOpt.Helpers;

namespace YaOpt.OtherMod.VanillaExpandedFramework.Patches.ThreadSafe
{
	[HarmonyPatch]
	[HarmonyAfter("OskarPotocki.VEF")]
	internal static class MultiTargets_VEFCooking
	{
		[ThreadStatic]
		public static bool Adjust;

		[ThreadStatic]
		public static HashSet<ThingDef> AlreadyUsed;

		private static Type _typeRecipeExtension;

		private static AccessTools.FieldRef<object, bool> _fieldIndividualIngredients;

		static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(WorkGiver_DoBill), "TryFindBestBillIngredientsInSet_AllowMix");
			yield return AccessTools.Method(typeof(WorkGiver_DoBill), "TryFindBestIngredientsInSet_NoMixHelper");
		}

		static bool Prepare(MethodBase original)
		{
			if (original != null)
				return true;

			var shouldRun = YaOptGlobal.Settings.OptParallelJobGiver.Enabled;

			if (shouldRun && _typeRecipeExtension == null)
			{
				_typeRecipeExtension = AccessTools.TypeByName("VEF.Cooking.Recipe_Extension");
				_fieldIndividualIngredients = AccessTools.FieldRefAccess<bool>(_typeRecipeExtension, "individualIngredients");
			}

			return shouldRun;
		}

		static void Prefix(Bill bill)
		{
			if (AlreadyUsed == null)
				AlreadyUsed = new HashSet<ThingDef>();
			AlreadyUsed.Clear();

			var adjust = false;
			var recipe = bill?.recipe;
			if (recipe != null && recipe.modExtensions != null)
			{
				foreach (var extension in recipe.modExtensions)
				{
					if (_typeRecipeExtension.IsInstanceOfType(extension))
					{
						adjust = _fieldIndividualIngredients(extension);
					}
					break;
				}
			}
			Adjust = adjust;
		}

		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				if (instruction.LoadsField("alreadyUsed", true))
				{
					instruction.operand = AccessTools.Field(
						typeof(MultiTargets_VEFCooking),
						nameof(AlreadyUsed));
				}
				else if (instruction.LoadsField("adjust", true))
				{
					instruction.operand = AccessTools.Field(
						typeof(MultiTargets_VEFCooking),
						nameof(Adjust));
				}
				yield return instruction;
			}
		}
	}
}