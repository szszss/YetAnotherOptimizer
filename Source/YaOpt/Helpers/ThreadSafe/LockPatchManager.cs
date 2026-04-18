using HarmonyLib;
using System;
using System.Reflection;
using System.Text;

namespace YaOpt.Helpers.ThreadSafe
{
	public static class LockPatchManager
	{
		public class PatchRequest : IEquatable<PatchRequest>
		{
			public readonly MethodBase TargetMethod;
			public readonly string LockKey;
			public readonly LockScope Scope;
			public readonly bool SupportRecursion;
			public readonly bool DetectDeadlock;

			public PatchRequest(MethodBase targetMethod, LockScope scope,
				bool supportRecursion = false, bool detectDeadlock = true, string lockKey = null)
			{
				TargetMethod = targetMethod;
				Scope = scope;
				SupportRecursion = supportRecursion;
				DetectDeadlock = detectDeadlock;
				LockKey = lockKey;
			}

			public bool Equals(PatchRequest other)
			{
				if (ReferenceEquals(null, other)) return false;
				if (ReferenceEquals(this, other)) return true;
				return Equals(TargetMethod, other.TargetMethod) && LockKey == other.LockKey && Scope == other.Scope &&
					   SupportRecursion == other.SupportRecursion && DetectDeadlock == other.DetectDeadlock;
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != this.GetType()) return false;
				return Equals((PatchRequest)obj);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					var hashCode = (TargetMethod != null ? TargetMethod.GetHashCode() : 0);
					hashCode = (hashCode * 397) ^ (LockKey != null ? LockKey.GetHashCode() : 0);
					hashCode = (hashCode * 397) ^ (int)Scope;
					hashCode = (hashCode * 397) ^ SupportRecursion.GetHashCode();
					hashCode = (hashCode * 397) ^ DetectDeadlock.GetHashCode();
					return hashCode;
				}
			}

			public static bool operator ==(PatchRequest left, PatchRequest right)
			{
				return Equals(left, right);
			}

			public static bool operator !=(PatchRequest left, PatchRequest right)
			{
				return !Equals(left, right);
			}

			public override string ToString()
			{
				var sb = new StringBuilder();
				sb.Append(TargetMethod.FullName());
				sb.Append(" (Scope=");
				switch (Scope)
				{
					case LockScope.Default:
						sb.Append("Auto");
						break;
					case LockScope.Key:
						sb.Append("CustomKey: ").Append(LockKey);
						break;
					case LockScope.Type:
						sb.Append("Type");
						break;
					case LockScope.Method:
						sb.Append("Method");
						break;
					case LockScope.Instance:
						sb.Append("Instance");
						break;
					default:
						sb.Append("Error");
						break;
				}
				if (SupportRecursion)
					sb.Append(", Supports recursion");
				if (DetectDeadlock)
					sb.Append(", Detects deadlock");
				sb.Append(" )");
				return sb.ToString();
			}
		}


		/// <summary>
		/// Dynamically generates a lock for the target method and applies the prefix and finalizer patches.
		/// </summary>
		public static void PatchMethod(Harmony harmony, PatchRequest request)
		{
			var patch = LockPatchGenerator.GetOrCreatePatchMethods(request.TargetMethod, request.Scope,
				request.SupportRecursion, request.DetectDeadlock, request.LockKey);

			harmony.Patch(request.TargetMethod,
				prefix: patch.PrefixMethod,
				finalizer: patch.FinalizerMethod
			);
		}
	}
}
