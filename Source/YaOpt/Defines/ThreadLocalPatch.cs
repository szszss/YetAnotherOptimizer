using HarmonyLib;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using Verse;

namespace YaOpt.Defines
{
	public class ThreadLocalPatch
	{
		private string _targetMethod;

		private string _fieldToReplace;

		private List<string> _methodParameters;

		[UsedImplicitly]
		public void LoadDataFromXmlCustom(XmlNode xmlRoot)
		{
			XmlNode elem = null;
			if ((elem = xmlRoot.SelectSingleNode("method")) != null)
			{
				_targetMethod = ParseHelper.FromString<string>(elem.InnerText);
			}
			if ((elem = xmlRoot.SelectSingleNode("replace")) != null)
			{
				_fieldToReplace = ParseHelper.FromString<string>(elem.InnerText);
			}
			if ((elem = xmlRoot.SelectSingleNode("parameters")) != null)
			{
				if (elem.HasChildNodes)
					_methodParameters = DirectXmlToObject.ObjectFromXml<List<string>>(elem, false);
				else
					_methodParameters = new List<string>(0);
			}
		}

		public (MethodInfo, string) Read(string owner)
		{
			Type[] tmpList = null;
			if (_methodParameters != null)
			{
				tmpList = new Type[_methodParameters.Count];
				for (var index = 0; index < _methodParameters.Count; index++)
				{
					var typeStr = _methodParameters[index];
					var type = ParseType(typeStr);
					tmpList[index] = type ?? throw new Exception($"Cannot find type {typeStr} for parameters");
				}
			}
			var method = AccessTools.Method(_targetMethod, tmpList);
			if (method == null)
				throw new Exception($"Cannot find method {_targetMethod}");
			if (string.IsNullOrWhiteSpace(_fieldToReplace))
				throw new Exception($"Invalid field to replace: {_fieldToReplace}");
			return (method, _fieldToReplace);
		}

		/// <summary>
		/// Parses a type string, supporting generic types like List&lt;int&gt;.
		/// </summary>
		private static Type ParseType(string typeStr)
		{
			if (string.IsNullOrWhiteSpace(typeStr))
				return null;

			typeStr = typeStr.Trim();

			// Check if this is a generic type (contains '<' and '>')
			var openBracket = typeStr.IndexOf('<');
			if (openBracket < 0)
			{
				// Not a generic type, use standard lookup
				return AccessTools.TypeByName(typeStr);
			}

			var closeBracket = typeStr.LastIndexOf('>');
			if (closeBracket < 0 || closeBracket <= openBracket)
				throw new Exception($"Invalid generic type syntax: {typeStr}");

			// Extract the generic type definition name (e.g., "List" from "List<int>")
			var genericDefName = typeStr.Substring(0, openBracket).Trim();

			// Extract the type arguments (e.g., "int" from "List<int>")
			var argsStr = typeStr.Substring(openBracket + 1, closeBracket - openBracket - 1);

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
				if (c == '<')
				{
					depth++;
					currentArg.Append(c);
				}
				else if (c == '>')
				{
					depth--;
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
	}
}