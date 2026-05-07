using System.Collections.Concurrent;
using Verse;
using Verse.AI;

namespace YaOpt.Helpers
{
	public static class ReservationPromiser
	{
		public static bool Working { get; private set; }

		private static readonly ConcurrentQueue<DelayedReservation> _promisedReservation = new ConcurrentQueue<DelayedReservation>();

		private readonly struct DelayedReservation
		{
			private readonly ReservationManager ReservationManager;
			private readonly Pawn Claimant;
			private readonly Job OwnerJob;
			private readonly LocalTargetInfo Target;
			private readonly int MaxPawns;
			private readonly int StackCount;
			private readonly ReservationLayerDef Layer;
			private readonly bool ErrorOnFailed;
			private readonly bool IgnoreOtherReservations;
			private readonly bool CanReserversStartJobs;

			public DelayedReservation(ReservationManager reservationManager, Pawn claimant, Job ownerJob,
				LocalTargetInfo target, int maxPawns, int stackCount, ReservationLayerDef layer,
				bool errorOnFailed, bool ignoreOtherReservations, bool canReserversStartJobs)
			{
				ReservationManager = reservationManager;
				Claimant = claimant;
				OwnerJob = ownerJob;
				Target = target;
				MaxPawns = maxPawns;
				StackCount = stackCount;
				Layer = layer;
				ErrorOnFailed = errorOnFailed;
				IgnoreOtherReservations = ignoreOtherReservations;
				CanReserversStartJobs = canReserversStartJobs;
			}

			public bool Check(Pawn pawn, Job job)
			{
				return pawn == Claimant && job == OwnerJob;
			}

			public void Fulfil()
			{
				ReservationManager.Reserve(Claimant, OwnerJob, Target, MaxPawns, StackCount,
					Layer, ErrorOnFailed, IgnoreOtherReservations, CanReserversStartJobs);
			}
		}

		public static void Start()
		{
			if (Working)
				YaOptMod.ErrorOnce("ReservationPromiser have already started.",
					typeof(ReservationPromiser).GetHashCode() + 1);
			Working = true;
		}

		public static void Stop()
		{
			Working = false;
		}

		public static void Promise(ReservationManager reservationManager, Pawn claimant, Job ownerJob,
			LocalTargetInfo target, int maxPawns, int stackCount, ReservationLayerDef layer,
			bool errorOnFailed, bool ignoreOtherReservations, bool canReserversStartJobs)
		{
			_promisedReservation.Enqueue(new DelayedReservation(reservationManager, claimant, ownerJob, target,
				maxPawns, stackCount, layer, errorOnFailed, ignoreOtherReservations, canReserversStartJobs));
		}

		public static void FulfilAndClear(Pawn pawn, Job job)
		{
			if (Working)
			{
				YaOptMod.ErrorOnce("ReservationPromiser must stop working before fulfilling the promises.",
					typeof(ReservationPromiser).GetHashCode());
				Working = false;
			}
			while (_promisedReservation.TryDequeue(out var promise))
			{
				if (promise.Check(pawn, job))
					promise.Fulfil();
			}
		}

		public static void Clear()
		{
			_promisedReservation.Clear();
		}
	}
}