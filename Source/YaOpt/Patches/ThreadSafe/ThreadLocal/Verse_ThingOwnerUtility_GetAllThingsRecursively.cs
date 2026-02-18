using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using YaOpt.Helpers;
using YaOpt.Helpers.ThreadLocal;

namespace YaOpt.Patches.ThreadSafe.ThreadLocal
{
	[HarmonyPatch]
	internal static class Verse_ThingOwnerUtility_GetAllThingsRecursively
	{
		static MethodBase TargetMethod()
		{
			return AccessTools.FirstMethod(
				typeof(ThingOwnerUtility),
				method => method.Name == "GetAllThingsRecursively" && method.IsGenericMethod)
				.MakeGenericMethod(typeof(Thing));
		}

		static bool Prepare()
		{
			return YaOptGlobal.NeedThreadSafe || YaOptGlobal.Settings.OptParallelJobGiver.Enabled;
		}

		static bool Prefix(Map __0, ThingRequest __1,
			object __2, bool __3 = true, Predicate<IThingHolder> __4 = null,
			bool __5 = true)
		{
			var listType = __2.GetType();
			var genericType = listType.GenericTypeArguments[0];

			if (genericType == typeof(Pawn))
			{
				ThreadLocalThingOwnerUtility.GetAllThingsRecursivelyGeneric(__0, __1, (List<Pawn>)__2, __3, __4, __5);
			}
			else if (genericType == typeof(Thing))
			{
				ThreadLocalThingOwnerUtility.GetAllThingsRecursively(__0, __1, (List<Thing>)__2, __3, __4, __5);
			}
			else
			{
				var parameters = ThreadLocalThingOwnerUtility.TmpParameters.Value;
				parameters[0] = __0;
				parameters[1] = __1;
				parameters[2] = __2;
				parameters[3] = __3;
				parameters[4] = __4;
				parameters[5] = __5;
				ThreadLocalThingOwnerUtility.GetAllThingsRecursivelyFindGenericMethod(genericType)
					.Invoke(null, parameters);
			}

			return false;
		}
	}
}