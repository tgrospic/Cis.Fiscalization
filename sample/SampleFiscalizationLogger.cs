using System.Xml;
using System.Diagnostics;

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
			Trace.WriteLine(request.OuterXml);
			Trace.WriteLine(response.OuterXml);
		}
	}
}
