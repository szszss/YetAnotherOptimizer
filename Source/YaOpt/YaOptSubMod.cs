using HarmonyLib;
using System;
using System.Collections.Generic;
using Verse;
using YaOpt.Settings;

namespace YaOpt
{
	public class YaOptSubMod
	{
		public virtual void OnPreInit()
		{
		}

		public virtual void OnInit()
		{
		}

		public virtual void OnPostInit()
		{
		}

		public virtual IEnumerable<OptimizationOption> OnCreateSettings()
		{
			yield break;
		}

		public virtual bool OnPrePatch(Harmony harmony)
		{
			return true;
		}


		public virtual bool OnPatch(Harmony harmony)
		{
			return true;
		}

		public virtual void OnUnpatch(Harmony harmony)
		{
		}

		internal static IEnumerable<YaOptSubMod> LoadAll()
		{
			foreach (var type in typeof(YaOptSubMod).AllSubclassesNonAbstract())
			{
				YaOptSubMod subMod = null;
				try
				{
					subMod = (YaOptSubMod)type.CreateInstance();
				}
				catch (Exception ex)
				{
					YaOptMod.Error($"Failed to load sub mod: {type.FullName}\n{ex}");
				}
				if (subMod != null)
					yield return subMod;
			}
		}

		internal static void PreInitAll(IEnumerable<YaOptSubMod> subMods)
		{
			foreach (var subMod in subMods)
			{
				try
				{
					subMod.OnPreInit();
				}
				catch (Exception ex)
				{
					YaOptMod.Error($"Failed to run OnPreInit for {subMod.GetType().FullName}\n{ex}");
				}
			}
		}

		internal static void InitAll(IEnumerable<YaOptSubMod> subMods)
		{
			foreach (var subMod in subMods)
			{
				try
				{
					subMod.OnInit();
				}
				catch (Exception ex)
				{
					YaOptMod.Error($"Failed to run OnInit for {subMod.GetType().FullName}\n{ex}");
				}
			}
		}

		internal static void PostInitAll(IEnumerable<YaOptSubMod> subMods)
		{
			foreach (var subMod in subMods)
			{
				try
				{
					subMod.OnPostInit();
				}
				catch (Exception ex)
				{
					YaOptMod.Error($"Failed to run OnPostInit for {subMod.GetType().FullName}\n{ex}");
				}
			}
		}

		internal static bool PrePatchAll(IEnumerable<YaOptSubMod> subMods, Harmony harmony)
		{
			var noError = true;
			foreach (var subMod in subMods)
			{
				try
				{
					noError &= subMod.OnPrePatch(harmony);
				}
				catch (Exception ex)
				{
					YaOptMod.Error(ex.ToString());
				}
			}
			return noError;
		}

		internal static bool PatchAll(IEnumerable<YaOptSubMod> subMods, Harmony harmony)
		{
			var noError = true;
			foreach (var subMod in subMods)
			{
				try
				{
					noError &= subMod.OnPatch(harmony);
				}
				catch (Exception ex)
				{
					YaOptMod.Error(ex.ToString());
				}
			}
			return noError;
		}

		internal static void UnpatchAll(IEnumerable<YaOptSubMod> subMods, Harmony harmony)
		{
			foreach (var subMod in subMods)
			{
				try
				{
					subMod.OnUnpatch(harmony);
				}
				catch (Exception ex)
				{
					YaOptMod.Error($"Failed to run OnUnpatch for {subMod.GetType().FullName}\n{ex}");
				}
			}
		}
	}
}