using Gilzoide.ManagedJobs;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Jobs;

namespace YaOpt.Helpers
{
	public class YaOptManagedJobs
	{
		private static class Handles<T>
		{
			private const int CAPACITY = 256;
			private static readonly T[] _handles = new T[CAPACITY];
			private static NativeBitArray _used = new NativeBitArray(CAPACITY, Allocator.Persistent);

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			internal static T Get(int index, in GCHandle backup)
			{
				if (index < 0)
					return backup.IsAllocated ? (T)backup.Target : default;
				return _handles[index];
			}

			internal static void Set(T target, out int index, out GCHandle backupHandle)
			{
				lock (_handles)
				{
					var i = _used.Find(0, 1);
					if (i >= 0)
					{
						_used.Set(i, true);
						_handles[i] = target;
						index = i;
						backupHandle = default;
						return;
					}
				}
				index = -1;
				backupHandle = GCHandle.Alloc(target);
			}

			internal static void Free(ref int index, ref GCHandle backup)
			{
				if (index >= 0)
				{
					lock (_handles)
					{
						_used.Set(index, false);
						_handles[index] = default;
					}
					index = -1;
				}
				if (backup.IsAllocated)
				{
					backup.Free();
					backup = default;
				}
			}
		}

		public struct Job : IJob, IDisposable
		{
			private int _handleIndex;
			private GCHandle _backupHandle;

			public Job(IJob job)
			{
				Handles<IJob>.Set(job, out _handleIndex, out _backupHandle);
			}

			public void Execute()
			{
				var job = Handles<IJob>.Get(_handleIndex, _backupHandle);
				if (job != null)
					job.Execute();
			}

			public void Dispose()
			{
				Handles<IJob>.Free(ref _handleIndex, ref _backupHandle);
			}

			public JobHandle Schedule(JobHandle dependsOn = default)
			{
				var jobHandle = IJobExtensions.Schedule(this, dependsOn);
				new DisposeJob<Job>(this).Schedule(jobHandle);
				return jobHandle;
			}

			public void Run()
			{
				try
				{
					IJobExtensions.Run(this);
				}
				finally
				{
					Dispose();
				}
			}
		}

		public struct JobFor : IJobFor, IDisposable
		{
			private int _handleIndex;
			private GCHandle _backupHandle;

			public JobFor(IJobFor job)
			{
				Handles<IJobFor>.Set(job, out _handleIndex, out _backupHandle);
			}

			public void Execute(int index)
			{
				var job = Handles<IJobFor>.Get(_handleIndex, _backupHandle);
				if (job != null)
					job.Execute(index);
			}

			public void Dispose()
			{
				Handles<IJobFor>.Free(ref _handleIndex, ref _backupHandle);
			}

			public JobHandle Schedule(int arrayLength, JobHandle dependsOn = default)
			{
				var jobHandle = IJobForExtensions.Schedule(this, arrayLength, dependsOn);
				new DisposeJob<JobFor>(this).Schedule(jobHandle);
				return jobHandle;
			}

			public JobHandle ScheduleParallel(int arrayLength, int innerloopBatchCount, JobHandle dependsOn = default)
			{
				var jobHandle = IJobForExtensions.ScheduleParallel(this, arrayLength, innerloopBatchCount, dependsOn);
				new DisposeJob<JobFor>(this).Schedule(jobHandle);
				return jobHandle;
			}

			public void Run(int arrayLength)
			{
				try
				{
					IJobForExtensions.Run(this, arrayLength);
				}
				finally
				{
					Dispose();
				}
			}
		}
	}
}