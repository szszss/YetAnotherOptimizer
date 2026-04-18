using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using Verse;
using YaOpt.Helpers;

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
			var method = MiscHelper.ParseMethod(_targetMethod, _methodParameters);
			if (string.IsNullOrWhiteSpace(_fieldToReplace))
				throw new Exception($"Invalid field to replace: {_fieldToReplace}");
			return (method, _fieldToReplace);
		}
	}
}