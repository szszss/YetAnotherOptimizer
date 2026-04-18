using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Unity.Jobs;
using UnityEngine;

namespace YaOpt.Helpers
{
	public static class MiscHelper
	{
		#region TypeInfo

		public static string DeclaringTypeName(this MethodBase method)
		{
			var type = method.DeclaringType;
			return type != null ? type.FullName : string.Empty;
		}

		public static string DeclaringTypeName(this FieldInfo field)
		{
			var type = field.DeclaringType;
			return type != null ? type.FullName : string.Empty;
		}

		public static string FullName(this MethodBase method)
		{
			return $"{method.DeclaringTypeName()}:{method.Name}";
		}

		public static string FullName(this FieldInfo field)
		{
			return $"{field.DeclaringTypeName()}:{field.Name}";
		}

		public static Hash128 GetMethodBodyHash(MethodBase method)
		{
			var instructions = PatchProcessor.GetOriginalInstructions(method);
			if (instructions == null)
				throw new MethodAccessException($"Cannot get the parsed IL instructions of method {method.FullName()}");

			using (var hasher = SHA1.Create())
			using (var ms = new System.IO.MemoryStream())
			{
				foreach (var inst in instructions)
				{
					var opValue = inst.opcode.Value;
					ms.WriteByte((byte)(opValue & 0xFF));
					if (inst.opcode.Size == 2)
					{
						ms.WriteByte((byte)((opValue >> 8) & 0xFF));
					}
				}

				ms.Position = 0;
				var hash = Hash128.Compute(hasher.ComputeHash(ms));
				YaOptMod.Debug($"MethodBody Hash: {method.FullName()} - {hash}");
				return hash;
			}
		}

		public static bool CheckHash(MethodBase method, Hash128 hash, bool throwException = false, bool warn = true)
		{
			try
			{
				var methodHash = GetMethodBodyHash(method);
				if (methodHash == hash)
					return true;

				if (warn)
				{
					YaOptMod.Warning($"Method {method.FullName()} code does not match the expected code.\n" +
									 $"Actual code hash: {methodHash}\n" +
									 $"Expected code hash: {hash}\n" +
									 "This may cause the patch to fail. " +
									 "This issue is caused by YaOpt. Please report it to the YaOpt developers.");
				}

				if (throwException)
					throw new Exception($"Method {method.FullName()} code does not match the expected code.\n" +
										$"Actual code hash: {methodHash}\n" +
										$"Expected code hash: {hash}\n" +
										"This issue is caused by YaOpt. Please report it to the YaOpt developers.");

			}
			catch (MethodAccessException)
			{
				if (warn)
					YaOptMod.Warning($"Cannot get the IL code of method {method.FullName()}");
				if (throwException)
					throw;
			}
			return false;
		}

		#endregion

		#region TypeResolver

		/// <summary>
		/// Maps C# type keywords to their corresponding .NET primitive types.
		/// </summary>
		private static readonly Dictionary<string, Type> TypeMapping = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
		{
			{ "bool", typeof(bool) },
			{ "byte", typeof(byte) },
			{ "sbyte", typeof(sbyte) },
			{ "char", typeof(char) },
			{ "decimal", typeof(decimal) },
			{ "double", typeof(double) },
			{ "float", typeof(float) },
			{ "int", typeof(int) },
			{ "uint", typeof(uint) },
			{ "long", typeof(long) },
			{ "ulong", typeof(ulong) },
			{ "short", typeof(short) },
			{ "ushort", typeof(ushort) },
			{ "object", typeof(object) },
			{ "string", typeof(string) },
			{ "void", typeof(void) },
		};

		public static MethodInfo ParseMethod(string methodStr, List<string> methodParameters)
		{
			Type[] tmpList = null;
			if (methodParameters != null)
			{
				tmpList = new Type[methodParameters.Count];
				for (var index = 0; index < methodParameters.Count; index++)
				{
					var typeStr = methodParameters[index];
					var type = ParseType(typeStr);
					tmpList[index] = type ?? throw new Exception($"Cannot find type {typeStr} for parameters");
				}
			}
			var method = AccessTools.Method(methodStr, tmpList);
			if (method == null)
			{
				var sb = new StringBuilder("Cannot find method ").Append(methodStr);
				if (tmpList != null)
				{
					sb.Append(" (Parameters count: ").Append(tmpList.Length);
					foreach (var type in tmpList)
					{
						sb.Append(' ').Append(type.FullName);
					}
					sb.Append(")");
				}
				throw new Exception(sb.ToString());
			}
			return method;
		}

		/// <summary>
		/// Parses a type string, supporting generic types like List(int) and arrays like int[].
		/// </summary>
		/// <remarks>
		/// Uses parentheses for generics (instead of angle brackets for easier XML authoring).
		/// Supports nested types: Dictionary(string, List(int[])), int[,], etc.
		/// Also supports C# type keywords: int, string, float, etc.
		/// Also supports out/ref modifiers: out int, ref string.
		/// </remarks>
		public static Type ParseType(string typeStr)
		{
			if (string.IsNullOrWhiteSpace(typeStr))
				return null;

			typeStr = typeStr.Trim();

			// Check for out/ref modifiers
			bool isByRef = false;
			if (typeStr.StartsWith("out ", StringComparison.OrdinalIgnoreCase) ||
				typeStr.StartsWith("ref ", StringComparison.OrdinalIgnoreCase))
			{
				isByRef = true;
				typeStr = typeStr.Substring(4).Trim();
			}

			Type type = ParseTypeCore(typeStr);

			if (isByRef && type != null)
				type = type.MakeByRefType();

			return type;
		}

		/// <summary>
		/// Core type parsing logic without out/ref handling.
		/// </summary>
		private static Type ParseTypeCore(string typeStr)
		{
			if (string.IsNullOrWhiteSpace(typeStr))
				return null;

			typeStr = typeStr.Trim();

			// Check for array suffix first (e.g., "int[]" or "List(int)[]")
			var arraySuffix = GetArraySuffix(typeStr, out var baseTypeStr);
			if (arraySuffix != null)
			{
				var elementType = ParseTypeCore(baseTypeStr);
				if (elementType == null)
					throw new Exception($"Cannot find element type for array: {baseTypeStr}");
				return arraySuffix.Length == 0
					? elementType.MakeArrayType()
					: elementType.MakeArrayType(arraySuffix.Length + 1);
			}

			// Check if this is a generic type (contains '(' and ')')
			var openParen = typeStr.IndexOf('(');
			if (openParen < 0)
			{
				// Not a generic type, use standard lookup with alias resolution
				return ResolveType(typeStr);
			}

			var closeParen = typeStr.LastIndexOf(')');
			if (closeParen < 0 || closeParen <= openParen)
				throw new Exception($"Invalid generic type syntax: {typeStr}");

			// Extract the generic type definition name (e.g., "List" from "List(int)")
			var genericDefName = typeStr.Substring(0, openParen).Trim();

			// Extract the type arguments (e.g., "int" from "List(int)")
			var argsStr = typeStr.Substring(openParen + 1, closeParen - openParen - 1);

			// Parse type arguments (may be nested generics)
			var typeArgs = ParseTypeArguments(argsStr);
			if (typeArgs == null || typeArgs.Count == 0)
				throw new Exception($"Failed to parse type arguments from: {typeStr}");

			// Get the generic type definition
			var genericDef = AccessTools.TypeByName(genericDefName + "`" + typeArgs.Count);
			if (genericDef == null)
			{
				// Fallback: try without the backtick count (some types may be found this way)
				genericDef = AccessTools.TypeByName(genericDefName);
				if (genericDef == null)
					throw new Exception($"Cannot find generic type definition: {genericDefName}");

				if (!genericDef.IsGenericTypeDefinition)
					throw new Exception($"Type {genericDefName} is not a generic type definition");
			}

			// Construct the generic type
			return genericDef.MakeGenericType(typeArgs.ToArray());
		}

		/// <summary>
		/// Resolves a type name, handling C# type aliases.
		/// </summary>
		private static Type ResolveType(string typeName)
		{
			if (TypeMapping.TryGetValue(typeName, out var primitiveType))
				return primitiveType;
			return AccessTools.TypeByName(typeName);
		}

		/// <summary>
		/// Extracts array suffix from type string, returns null if not an array.
		/// </summary>
		/// <param name="typeStr">Full type string (e.g., "int[]" or "int[,,]")</param>
		/// <param name="baseTypeStr">Output: the element type string without array suffix</param>
		/// <returns>Array rank indicators (e.g., "" for 1D, ",," for 3D), or null if not an array</returns>
		private static string GetArraySuffix(string typeStr, out string baseTypeStr)
		{
			baseTypeStr = typeStr;

			// Find array brackets at the end
			var lastBracket = typeStr.LastIndexOf(']');
			if (lastBracket < 0)
				return null;

			// Find matching opening bracket
			var openBracket = typeStr.LastIndexOf('[');
			if (openBracket < 0 || openBracket >= lastBracket)
				return null;

			// Ensure the brackets are at the end
			if (lastBracket != typeStr.Length - 1)
				return null;

			// Extract the suffix content (e.g., "" for [], ",," for [,,,])
			var suffix = typeStr.Substring(openBracket + 1, lastBracket - openBracket - 1);

			// Validate: should only contain commas
			foreach (var c in suffix)
			{
				if (c != ',')
					return null;
			}

			baseTypeStr = typeStr.Substring(0, openBracket).Trim();
			return suffix;
		}

		/// <summary>
		/// Parses comma-separated type arguments, handling nested generics.
		/// </summary>
		private static List<Type> ParseTypeArguments(string argsStr)
		{
			var result = new List<Type>();
			var currentArg = new System.Text.StringBuilder();
			var depth = 0;

			for (var i = 0; i < argsStr.Length; i++)
			{
				var c = argsStr[i];
				if (c == '(')
				{
					depth++;
					currentArg.Append(c);
				}
				else if (c == ')')
				{
					depth--;
					currentArg.Append(c);
				}
				else if (c == '[' || c == ']')
				{
					// Allow array syntax inside type arguments
					currentArg.Append(c);
				}
				else if (c == ',' && depth == 0)
				{
					// This comma separates type arguments at the current level
					var arg = currentArg.ToString().Trim();
					if (!string.IsNullOrEmpty(arg))
					{
						var type = ParseType(arg);
						if (type != null)
							result.Add(type);
					}
					currentArg.Clear();
				}
				else
				{
					currentArg.Append(c);
				}
			}

			// Don't forget the last argument
			var lastArg = currentArg.ToString().Trim();
			if (!string.IsNullOrEmpty(lastArg))
			{
				var type = ParseType(lastArg);
				if (type != null)
					result.Add(type);
			}

			return result;
		}

		#endregion

		public static bool ReverseRemove<T>(this List<T> list, T t)
		{
			var index = list.LastIndexOf(t);
			if (index >= 0)
			{
				list.RemoveAt(index);
				return true;
			}
			return false;
		}

		public static void CompleteWithSpinWait(this JobHandle jobHandle)
		{
			if (jobHandle.Equals(default))
				return;
			while (!jobHandle.IsCompleted)
			{
				Thread.SpinWait(10);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static long GetElapsedMicrosecondLong(this Stopwatch stopwatch)
		{
			return (long)GetElapsedMicrosecond(stopwatch);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static double GetElapsedMicrosecond(this Stopwatch stopwatch)
		{
			return (double)stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency;
		}
	}
}