using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;

namespace YaOpt.Helpers.ThreadSafe
{
	public static class LockPatchGenerator
	{
		private static readonly ModuleBuilder _moduleBuilder;

		private static readonly Dictionary<MethodBase, PatchInfo> _cache =
			new Dictionary<MethodBase, PatchInfo>();

		private static readonly Dictionary<string, int> _lockWrapperLookup = new Dictionary<string, int>();

		private static readonly List<LockWrapper> _lockWrappers = new List<LockWrapper>();

		private static readonly MethodInfo _methodGreedySpinLockEnter;

		private static readonly MethodInfo _methodGreedySpinLockExit;

		private static readonly MethodInfo _methodGreedyMonitorEnter;

		private static readonly MethodInfo _methodGreedyMonitorExit;

		private static readonly MethodInfo _methodGreedySpinLockSupportRecursionSetter;

		private static readonly MethodInfo _methodGreedySpinLockDetectDeadlockSetter;

		private static readonly MethodInfo _methodLockWrapperEnter;

		private static readonly MethodInfo _methodLockWrapperExit;

		private static readonly MethodInfo _methodGetLockWrapper;

		private static int _typeCounter = 0;

		public sealed class PatchInfo
		{
			public readonly Type PatchType;
			public readonly MethodInfo PrefixMethod;
			public readonly MethodInfo FinalizerMethod;

			internal PatchInfo(Type patchType, MethodInfo prefixMethod, MethodInfo finalizerMethod)
			{
				PatchType = patchType;
				PrefixMethod = prefixMethod;
				FinalizerMethod = finalizerMethod;
			}
		}

		private sealed class LockWrapper
		{
			private GreedySpinLock _spinLock = new GreedySpinLock();

			public bool SupportRecursion
			{
				get => _spinLock.SupportRecursion;
				set => _spinLock.SupportRecursion = value;
			}

			public bool DetectDeadlock
			{
				get => _spinLock.DetectDeadlock;
				set => _spinLock.DetectDeadlock = value;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Enter()
			{
				_spinLock.Enter();
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Exit()
			{
				_spinLock.Exit();
			}
		}

		static LockPatchGenerator()
		{
			var assemblyName = new AssemblyName("YaOpt.DynamicLocks");
			var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
			_moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicLocksModule");
			_methodGreedySpinLockEnter = AccessTools.Method(
				typeof(GreedySpinLock),
				nameof(GreedySpinLock.Enter),
				new[] { typeof(bool).MakeByRefType() });
			_methodGreedySpinLockExit = AccessTools.Method(
				typeof(GreedySpinLock),
				nameof(GreedySpinLock.Exit));
			_methodGreedyMonitorEnter = AccessTools.Method(
				typeof(GreedyMonitor),
				nameof(GreedyMonitor.Enter),
				new[] { typeof(object), typeof(bool).MakeByRefType(), typeof(bool) });
			_methodGreedyMonitorExit = AccessTools.Method(
				typeof(GreedyMonitor),
				nameof(GreedyMonitor.Exit));
			_methodGreedySpinLockSupportRecursionSetter =
				AccessTools.PropertySetter(typeof(GreedySpinLock),
					nameof(GreedySpinLock.SupportRecursion));
			_methodGreedySpinLockDetectDeadlockSetter =
				AccessTools.PropertySetter(typeof(GreedySpinLock),
					nameof(GreedySpinLock.DetectDeadlock));
			_methodLockWrapperEnter = AccessTools.Method(
				typeof(LockWrapper),
				nameof(LockWrapper.Enter));
			_methodLockWrapperExit = AccessTools.Method(
				typeof(LockWrapper),
				nameof(LockWrapper.Exit));
			_methodGetLockWrapper = AccessTools.Method(
				typeof(LockPatchGenerator),
				nameof(GetLock));
		}

		public static PatchInfo GetOrCreatePatchMethods(MethodBase targetMethod, LockScope lockScope,
			bool supportRecursion = false, bool detectDeadlock = true, string lockKey = null)
		{
			lock (_cache)
			{
				if (_cache.TryGetValue(targetMethod, out var cached))
				{
					return cached;
				}

				var generated = Generate(targetMethod, lockScope, supportRecursion, detectDeadlock, lockKey);
				_cache[targetMethod] = generated;
				return generated;
			}
		}

		private static PatchInfo Generate(MethodBase targetMethod, LockScope lockScope,
			bool supportRecursion = false, bool detectDeadlock = true, string lockKey = null)
		{
			var targetType = targetMethod.DeclaringType;
			var declaringTypeName = targetType?.Name ?? "UnknownType";
			var methodName = targetMethod.Name.Replace(".", "_").Replace("<", "").Replace(">", "");
			var typeName = $"YaOptDynamicLock_{declaringTypeName}_{methodName}_{Interlocked.Increment(ref _typeCounter)}";

			var typeBuilder = _moduleBuilder.DefineType(
				typeName,
				TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.Abstract);

			Type fieldType = null;
			FieldBuilder lockField = null;
			int lockIndex = -1;
			if (lockScope == LockScope.Default)
			{
				lockScope = targetMethod.IsStatic ? LockScope.Method : LockScope.Instance;
			}
			switch (lockScope)
			{
				case LockScope.Key:
				case LockScope.Type:
					if (lockScope == LockScope.Type)
					{
						if (targetType == null)
							throw new MissingMemberException(
								$"Method {targetMethod.FullName()} use a type-scoped lock, but cannot find its declaring type.");
						lockKey = "___" + targetType.FullName;
					}
					else if (string.IsNullOrWhiteSpace(lockKey))
					{
						throw new ArgumentNullException(nameof(lockKey),
							$"Method {targetMethod.FullName()} use a custom key lock, but the key name is missing.");
					}
					lockIndex = GetOrCreateLockWrapper(lockKey, supportRecursion, detectDeadlock);
					break;
				case LockScope.Method:
					fieldType = typeof(GreedySpinLock);
					lockField = typeBuilder.DefineField("_lockObj", fieldType,
						FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly);
					break;
				case LockScope.Instance:
					if (targetMethod.IsStatic)
						throw new ArgumentException(
							$"Static method {targetMethod.FullName()} doesn't support instance-scoped lock", nameof(lockScope));
					if (targetType == null)
						throw new MissingMemberException(
							$"Method {targetMethod.FullName()} requires a instance-scoped lock, but cannot find its declaring type.");
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(lockScope), lockScope, null);
			}

			// --- Static Constructor (.cctor) ---
			var cctor = typeBuilder.DefineConstructor(
				MethodAttributes.Static | MethodAttributes.Private | MethodAttributes.HideBySig,
				CallingConventions.Standard, Type.EmptyTypes);
			var ilCctor = cctor.GetILGenerator();

			if (lockScope == LockScope.Method)
			{
				ilCctor.Emit(OpCodes.Ldsflda, lockField);
				ilCctor.Emit(OpCodes.Initobj, fieldType);

				ilCctor.Emit(OpCodes.Ldsflda, lockField);
				ilCctor.Emit(supportRecursion ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
				ilCctor.Emit(OpCodes.Call, _methodGreedySpinLockSupportRecursionSetter);

				ilCctor.Emit(OpCodes.Ldsflda, lockField);
				ilCctor.Emit(detectDeadlock ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
				ilCctor.Emit(OpCodes.Call, _methodGreedySpinLockDetectDeadlockSetter);
			}
			ilCctor.Emit(OpCodes.Ret);

			// --- Prefix Method ---
			var prefixBuilder = typeBuilder.DefineMethod(
				"Prefix",
				MethodAttributes.Public | MethodAttributes.Static,
				typeof(void),
				lockScope == LockScope.Instance
					? new[] { typeof(bool).MakeByRefType(), targetType }
					: new[] { typeof(bool).MakeByRefType() });

			prefixBuilder.DefineParameter(1, ParameterAttributes.Out, "__state");
			if (lockScope == LockScope.Instance)
			{
				prefixBuilder.DefineParameter(2, ParameterAttributes.None, "__instance");
			}

			var ilPrefix = prefixBuilder.GetILGenerator();

			// __state = false;
			ilPrefix.Emit(OpCodes.Ldarg_0);
			ilPrefix.Emit(OpCodes.Ldc_I4_0);
			ilPrefix.Emit(OpCodes.Stind_I1);

			switch (lockScope)
			{
				case LockScope.Key:
				case LockScope.Type:
					// GetLock(lockIndex).Enter();
					ilPrefix.Emit(OpCodes.Ldc_I4, lockIndex);
					ilPrefix.Emit(OpCodes.Call, _methodGetLockWrapper);
					ilPrefix.Emit(OpCodes.Call, _methodLockWrapperEnter);
					// __state = true;
					ilPrefix.Emit(OpCodes.Ldarg_0);
					ilPrefix.Emit(OpCodes.Ldc_I4_1);
					ilPrefix.Emit(OpCodes.Stind_I1);
					break;
				case LockScope.Method:
					ilPrefix.Emit(OpCodes.Ldsflda, lockField); // _lockObj
					ilPrefix.Emit(OpCodes.Ldarg_0); // ref __state
					ilPrefix.Emit(OpCodes.Call, _methodGreedySpinLockEnter);
					break;
				case LockScope.Instance:
					ilPrefix.Emit(OpCodes.Ldarg_1); // __instance
					ilPrefix.Emit(OpCodes.Ldarg_0); // ref __state
					ilPrefix.Emit(detectDeadlock ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0); // detectDeadlock
					ilPrefix.Emit(OpCodes.Call, _methodGreedyMonitorEnter);
					break;
			}
			ilPrefix.Emit(OpCodes.Ret);

			// --- Finalizer Method ---
			var finalizerBuilder = typeBuilder.DefineMethod(
				"Finalizer",
				MethodAttributes.Public | MethodAttributes.Static,
				typeof(void),
				lockScope == LockScope.Instance
					? new[] { typeof(bool).MakeByRefType(), targetType }
					: new[] { typeof(bool).MakeByRefType() });

			finalizerBuilder.DefineParameter(1, ParameterAttributes.None, "__state");
			if (lockScope == LockScope.Instance)
			{
				finalizerBuilder.DefineParameter(2, ParameterAttributes.None, "__instance");
			}

			var ilFinalizer = finalizerBuilder.GetILGenerator();
			var skipExitLabel = ilFinalizer.DefineLabel();

			// if (!__state) return;
			ilFinalizer.Emit(OpCodes.Ldarg_0);
			ilFinalizer.Emit(OpCodes.Brfalse_S, skipExitLabel);

			switch (lockScope)
			{
				case LockScope.Key:
				case LockScope.Type:
					// GetLock(lockIndex).Exit();
					ilFinalizer.Emit(OpCodes.Ldc_I4, lockIndex);
					ilFinalizer.Emit(OpCodes.Call, _methodGetLockWrapper);
					ilFinalizer.Emit(OpCodes.Call, _methodLockWrapperExit);
					break;
				case LockScope.Method:
					ilFinalizer.Emit(OpCodes.Ldsflda, lockField); // _lockObj
					ilFinalizer.Emit(OpCodes.Call, _methodGreedySpinLockExit);
					break;
				case LockScope.Instance:
					ilFinalizer.Emit(OpCodes.Ldarg_1); // __instance
					ilFinalizer.Emit(OpCodes.Call, _methodGreedyMonitorExit);
					break;
			}

			ilFinalizer.MarkLabel(skipExitLabel);
			ilFinalizer.Emit(OpCodes.Ret);

			var createdType = typeBuilder.CreateType();
			return new PatchInfo(createdType, createdType.GetMethod("Prefix"), createdType.GetMethod("Finalizer"));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static LockWrapper GetLock(int lockIndex)
		{
			return _lockWrappers[lockIndex];
		}

		private static int GetOrCreateLockWrapper(string lockKey, bool supportRecursion, bool detectDeadlock)
		{
			LockWrapper lockObj;
			if (_lockWrapperLookup.TryGetValue(lockKey, out var lockIndex))
			{
				lockObj = GetLock(lockIndex);
				if (supportRecursion && !lockObj.SupportRecursion)
				{
					lockObj.SupportRecursion = true;
					YaOptMod.Debug($"Upgrade the lock {lockKey} to support recursion.");
				}
				if (detectDeadlock && !lockObj.DetectDeadlock)
				{
					lockObj.DetectDeadlock = true;
					YaOptMod.Debug($"Upgrade the lock {lockKey} to detect deadlock.");
				}
				return lockIndex;
			}
			lockObj = new LockWrapper()
			{
				DetectDeadlock = detectDeadlock,
				SupportRecursion = supportRecursion
			};
			lockIndex = _lockWrappers.Count;
			_lockWrappers.Add(lockObj);
			_lockWrapperLookup[lockKey] = lockIndex;
			return lockIndex;
		}
	}
}
