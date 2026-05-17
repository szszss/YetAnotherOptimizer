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
			foreach (XmlNode child in xmlRoot.ChildNodes)
			{
				switch (child.Name)
				{
				case "method":
					_targetMethod = ParseHelper.FromString<string>(child.InnerText);
					break;
				case "replace":
					_fieldToReplace = ParseHelper.FromString<string>(child.InnerText);
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

		public (MethodInfo, string) Read(string owner)
		{
			var method = MiscHelper.ParseMethod(_targetMethod, _methodParameters);
			if (string.IsNullOrWhiteSpace(_fieldToReplace))
				throw new Exception($"Invalid field to replace: {_fieldToReplace}");
			return (method, _fieldToReplace);
		}
	}
}