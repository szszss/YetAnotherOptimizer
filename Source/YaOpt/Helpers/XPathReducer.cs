using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Xml;

namespace YaOpt.Helpers
{
	internal static class XPathReducer
	{
		private const string IDENTITY = @"[a-zA-Z_][\w\-\.]";
		private const string DEF_NAME = "defName";
		private const string ANY_SPACE = @"\s*";
		private const string EQUALITY = ANY_SPACE + "=" + ANY_SPACE;

		private static Dictionary<string, Dictionary<string, XmlElement>> xmlCache;
		private static XmlElement xmlDefs;
		private static Queue<(XmlElement, int)> tmpQueue;
		private static NodeEnumerator singletonEnumerator;
		private static bool available;

		private static readonly Regex xPathRegex = new Regex(
			"^" +
			"(?:/(" + IDENTITY + "*" +
			"(?:" + @"\[" + ANY_SPACE + IDENTITY + "+" + EQUALITY + @"""[^""]*""" + ANY_SPACE + @"\])?" +
			"))+$",
			RegexOptions.Compiled);

		private static readonly Regex queryRegex = new Regex(
			"^(?:(" + IDENTITY + @"*)\[" + ANY_SPACE + "(" + IDENTITY + "+)" + EQUALITY + 
			@"""([^""]*)""" + ANY_SPACE + @"\])$",
			RegexOptions.Compiled);

		private class NodeEnumerator : IEnumerator
		{
			public bool MoveNext()
			{
				return tmpQueue.Count > 0;
			}

			public void Reset()
			{
			}

			public object Current => tmpQueue.Dequeue().Item1;
		}

		public static void CreateCache(XmlDocument xml)
		{
			var nameSet = new HashSet<(string, string)>();
			xmlCache = new Dictionary<string, Dictionary<string, XmlElement>>();
			xmlDefs = xml["Defs"];
			tmpQueue = new Queue<(XmlElement, int)>();
			singletonEnumerator = new NodeEnumerator();
			foreach (object obj in xmlDefs.ChildNodes)
			{
				if (!(obj is XmlElement def))
					continue;
				var defKind = def.Name;
				if (!xmlCache.TryGetValue(defKind, out var defCacheOfKind))
				{
					defCacheOfKind = new Dictionary<string, XmlElement>();
					xmlCache[defKind] = defCacheOfKind;
				}
				var defNameNode = def["defName"];
				if (defNameNode != null)
				{
					var name = defNameNode.InnerText;
					// It will only cache the defs which have unique name.
					var nameTuple = (defKind, name);
					if (nameSet.Add(nameTuple))
						defCacheOfKind[name] = def;
					else
						defCacheOfKind.Remove(name);
				}
			}
			available = true;
		}

		public static void ClearCache()
		{
			available = false;
			xmlCache = null;
			xmlDefs = null;
			tmpQueue = null;
			singletonEnumerator = null;
			GC.Collect(0, GCCollectionMode.Optimized, false);
		}

		[SuppressMessage("ReSharper", "PossibleNullReferenceException")]
		[SuppressMessage("ReSharper", "NotDisposedResourceIsReturned")]
		public static IEnumerator GetXmlEnumerator(XmlDocument xml, string xpath)
		{
			if (!available)
				return xml.SelectNodes(xpath).GetEnumerator();

			var shouldFallback = true;
			if (xpath.StartsWith("Defs/", StringComparison.Ordinal))
				xpath = "/" + xpath;
			try
			{
				tmpQueue.Clear();
				var matches = xPathRegex.Match(xpath);
				if (matches.Success)
				{
					shouldFallback = false;
					var captures = matches.Groups[1].Captures;
					var count = captures.Count;
					if (captures[0].Value == "Defs")
					{
						tmpQueue.Enqueue((xmlDefs, 0));

						for (var i = 1; i < count; i++)
						{
							if (tmpQueue.Count == 0)
							{
								break;
							}
							var capture = captures[i].Value;
							var queryMatches = queryRegex.Match(capture);
							if (queryMatches.Success)
							{
								var nodeName = queryMatches.Groups[1].Value;
								var queryName = queryMatches.Groups[2].Value;
								var queryValue = queryMatches.Groups[3].Value;
								if (i == 1 && queryName == "defName" && 
								    xmlCache.TryGetValue(nodeName, out var dict) &&
								    dict.TryGetValue(queryValue, out var nextNode) &&
								    nextNode.ParentNode != null)
								{
									tmpQueue.Dequeue();
									tmpQueue.Enqueue((nextNode, 1));
								}
								else
								{
									while (tmpQueue.Count > 0 && tmpQueue.Peek().Item2 == i - 1)
									{
										var parentNode = tmpQueue.Dequeue().Item1;
										foreach (object obj in parentNode.ChildNodes)
										{
											if (!(obj is XmlElement childNode))
												continue;

											if (childNode.Name == nodeName)
											{
												var queryNode = childNode[queryName];
												if (queryNode != null && queryNode.InnerText == queryValue)
												{
													tmpQueue.Enqueue((childNode, i));
												}
											}
										}
									}
								}
							}
							else
							{
								while (tmpQueue.Count > 0 && tmpQueue.Peek().Item2 == i - 1)
								{
									var parentNode = tmpQueue.Dequeue().Item1;
									foreach (object obj in parentNode.ChildNodes)
									{
										if (!(obj is XmlElement childNode))
											continue;

										if (childNode.Name == capture)
										{
											tmpQueue.Enqueue((childNode, i));
										}
									}
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				shouldFallback = true;
				tmpQueue.Clear();
				YaOptMod.Error("Error when optimized patching. Fallback to vanilla method. " +
				               $"Exception: {ex}");
			}

			if (!shouldFallback)
			{
				singletonEnumerator.Reset();
				return singletonEnumerator;
			}
			//YaOptMod.Warning($"Fallback xpath: {xpath}");
			return xml.SelectNodes(xpath).GetEnumerator();
		}

		public static XmlNode GetXmlFirstNode(XmlDocument xml, string xpath)
		{
			var enumerator = GetXmlEnumerator(xml, xpath);
			if (enumerator.MoveNext())
				return enumerator.Current as XmlNode;
			return null;
		}
	}
}