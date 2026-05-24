using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Xml;
using Verse;
using YaOpt.Helpers;
using YaOpt.Helpers.ThreadSafe;

namespace YaOpt.Defines
{
	public class LockPatch
	{
		private string _targetMethod;

		private string _key;

		private List<string> _methodParameters;

		private LockScope _scope = LockScope.Default;

		private bool _supportRecursion;

		private bool _detectDeadlock = true;

		[UsedImplicitly]
		public void LoadDataFromXmlCustom(XmlNode xmlRoot)
		{
			foreach (XmlNode child in xmlRoot.ChildNodes)
			{
				switch (child.Name)
				{
					case "method":
						_targetMethod = ParseHelper.FromString<string>(child.InnerText);
						break;
					case "key":
						_key = ParseHelper.FromString<string>(child.InnerText);
						break;
					case "scope":
						_scope = Enum.Parse<LockScope>(child.InnerText, true);
						break;
					case "supportRecursion":
						_supportRecursion = ParseHelper.ParseBool(child.InnerText);
						break;
					case "detectDeadlock":
						_detectDeadlock = ParseHelper.ParseBool(child.InnerText);
						break;
					case "parameters":
						if (child.HasChildNodes)
							_methodParameters = DirectXmlToObject.ObjectFromXml<List<string>>(child, false);
						else
							_methodParameters = new List<string>(0);
						break;
				}
			}
		}

		public LockPatchManager.PatchRequest Read(string owner)
		{
			var method = MiscHelper.ParseMethod(_targetMethod, _methodParameters);
			return new LockPatchManager.PatchRequest(method, _scope, _supportRecursion, _detectDeadlock, _key);
		}
	}
}