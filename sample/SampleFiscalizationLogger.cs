using System.Xml;
using System.Diagnostics;
using System.IO;

// Must be defined in same project with Cis.FiskalizacijaService class
namespace Cis
{
	/// <summary>
	/// Sample trace implementation of fiscalization logger
	/// </summary>
	public partial class FiskalizacijaService
	{
		partial void LogResponseRaw(XmlDocument request, XmlDocument response)
		{
			// Trace logger
			Trace.WriteLine(request.OuterXml);
			Trace.WriteLine(response.OuterXml);

			// File logger
			File.AppendAllText(logFileName, request.OuterXml, Encoding.UTF8);
			File.AppendAllText(logFileName, response.OuterXml, Encoding.UTF8);
		}
	}
}
