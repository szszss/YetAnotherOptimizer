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
			XmlNode elem = null;
			if ((elem = xmlRoot.SelectSingleNode("method")) != null)
			{
				_targetMethod = ParseHelper.FromString<string>(elem.InnerText);
			}
			if ((elem = xmlRoot.SelectSingleNode("key")) != null)
			{
				_key = ParseHelper.FromString<string>(elem.InnerText);
			}
			if ((elem = xmlRoot.SelectSingleNode("scope")) != null)
			{
				_scope = Enum.Parse<LockScope>(elem.InnerText, true);
			}
			if ((elem = xmlRoot.SelectSingleNode("supportRecursion")) != null)
			{
				_supportRecursion = ParseHelper.ParseBool(elem.InnerText);
			}
			if ((elem = xmlRoot.SelectSingleNode("detectDeadlock")) != null)
			{
				_detectDeadlock = ParseHelper.ParseBool(elem.InnerText);
			}
			if ((elem = xmlRoot.SelectSingleNode("parameters")) != null)
			{
				if (elem.HasChildNodes)
					_methodParameters = DirectXmlToObject.ObjectFromXml<List<string>>(elem, false);
				else
					_methodParameters = new List<string>(0);
			}

		}

		public LockPatchManager.PatchRequest Read(string owner)
		{
			var method = MiscHelper.ParseMethod(_targetMethod, _methodParameters);
			return new LockPatchManager.PatchRequest(method, _scope, _supportRecursion, _detectDeadlock, _key);
		}
	}
}