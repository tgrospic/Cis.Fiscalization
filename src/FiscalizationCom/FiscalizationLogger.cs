using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace Cis
{
	public partial class FiskalizacijaService
	{
		public string LogFileName { get; set; }

		object _locker = new object();

		partial void LogResponseRaw(XmlDocument request, XmlDocument response)
		{
			// File logger
			if (LogFileName != null)
			{
				var sb = new StringBuilder();
				sb.AppendLine(request.DocumentElement.OuterXml);
				sb.AppendLine(response.DocumentElement.OuterXml);

				lock (_locker)
				{
					File.AppendAllText(LogFileName, sb.ToString(), Encoding.UTF8);
				}
			}
		}
	}
}
