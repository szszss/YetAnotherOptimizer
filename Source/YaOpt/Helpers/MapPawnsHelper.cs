using RimWorld;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Verse;
using YaOpt.Helpers.ThreadLocal;

namespace YaOpt.Helpers
{
	public static class MapPawnsHelper
	{
		private static readonly List<Pawn> _emptyList = new List<Pawn>(0);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsFreeHumanlike(Pawn pawn, Faction faction)
		{
			return (!ModsConfig.AnomalyActive || !pawn.IsSubhuman) && pawn.Faction == faction &&
				   (pawn.HostFaction == null || pawn.IsSlave) && pawn.RaceProps.Humanlike;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsColonySubhumanControllable(Pawn pawn, Faction faction)
		{
			return pawn.Faction == faction && pawn.IsColonySubhuman && pawn.mutant.Def.canBeDrafted;
		}

		public static List<Pawn> Nothing()
		{
			return _emptyList;
		}

		public static List<Pawn> Nothing(MapPawns _)
		{
			return _emptyList;
		}

		public static List<Pawn> FreeColonistsAndSubhumansControllable(MapPawns mapPawns)
		{
			var faction = Faction.OfPlayer;
			var pawnList = ThreadLocalMapPawns.GetPooledList();
			pawnList.Clear();
			foreach (var pawn in mapPawns.AllPawns)
			{
				if (IsFreeHumanlike(pawn, faction) || IsColonySubhumanControllable(pawn, faction))
				{
					pawnList.Add(pawn);
				}
			}
			return pawnList;
		}

		public static List<Pawn> FreeColonistsAndPrisoners(MapPawns mapPawns)
		{
			var faction = Faction.OfPlayer;
			var pawnList = ThreadLocalMapPawns.GetPooledList();
			pawnList.Clear();
			foreach (var pawn in mapPawns.AllPawns)
			{
				if (IsFreeHumanlike(pawn, faction) || pawn.IsPrisonerOfColony)
				{
					pawnList.Add(pawn);
				}
			}
			return pawnList;
		}

		public static List<Pawn> AllMapsCaravansAndTravellingTransporters_Alive_FreeColonistsAndPrisoners()
		{
			var pawnList = ThreadLocalMapPawns.GetPooledList();
			pawnList.Clear();
			foreach (var pawn in PawnsFinder.AllMaps)
			{
				if (pawn.IsFreeColonist || pawn.IsPrisonerOfColony)
				{
					pawnList.Add(pawn);
				}
			}
			foreach (var pawn in PawnsFinder.AllCaravansAndTravellingTransporters_Alive)
			{
				if (pawn.IsFreeColonist || pawn.IsPrisonerOfColony)
				{
					pawnList.Add(pawn);
				}
			}
			return pawnList;
		}
	}
}