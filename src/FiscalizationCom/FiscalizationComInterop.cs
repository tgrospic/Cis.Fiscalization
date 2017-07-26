using Cis;
using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;

public class FiscalizationComInterop
{
	static FiscalizationComInterop() {
		// Explicitly set TLS security protocol (from .NET 4.5 TLS1.1 & TLS1.2)
		// Specified in CIS 1.3 `3.2 Sigurnosni preduvjeti` min. TLS1.1 | TLS1.2
		ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
	}

	#region Properties

	public string LogFileName { get; set; }

	#endregion

	#region Service methods

	/// <summary>
	/// Send invoice request
	/// </summary>
	/// <param name="request">Request to send</param>
	/// <param name="certificate">Signing certificate</param>
	/// <param name="timeout">Timeout in miliseconds, 0(default) = 100sec.</param>
	/// <param name="isDemo">Connect to production or demo CIS service</param>
	/// <param name="checkResponseSignature">Check if response signature is valid</param>
	/// <returns>Response from CIS service</returns>
	public RacunOdgovor SendInvoiceRequest(RacunZahtjev request, X509Certificate2 certificate,
		int timeout = 0, bool isDemo = false, bool checkResponseSignature = true)
	{
		// Send request
		return Fiscalization.SendInvoiceRequest(request, certificate, ServiceSetupHandler(timeout, isDemo, checkResponseSignature));
	}

	/// <summary>
	/// Send invoice
	/// </summary>
	/// <param name="invoice">Invoice to send</param>
	/// <param name="certificate">Signing certificate</param>
	/// <param name="timeout">Timeout in miliseconds, 0(default) = 100sec.</param>
	/// <param name="isDemo">Connect to production or demo CIS service</param>
	/// <param name="checkResponseSignature">Check if response signature is valid</param>
	/// <returns>Response from CIS service</returns>
	public RacunOdgovor SendInvoice(RacunType invoice, X509Certificate2 certificate,
		int timeout = 0, bool isDemo = false, bool checkResponseSignature = true)
	{
		// Send request
		return Fiscalization.SendInvoice(invoice, certificate, ServiceSetupHandler(timeout, isDemo, checkResponseSignature));
	}

	/// <summary>
	/// Check invoice request
	/// </summary>
	/// <param name="request">Request to check</param>
	/// <param name="certificate">Signing certificate</param>
	/// <param name="timeout">Timeout in miliseconds, 0(default) = 100sec.</param>
	/// <param name="isDemo">Connect to production or demo CIS service</param>
	/// <param name="checkResponseSignature">Check if response signature is valid</param>
	/// <returns>Response from CIS service</returns>
	public ProvjeraOdgovor CheckInvoiceRequest(ProvjeraZahtjev request, X509Certificate2 certificate,
		int timeout = 0, bool isDemo = false, bool checkResponseSignature = true)
	{
		// Send request
		return Fiscalization.CheckInvoiceRequest(request, certificate, ServiceSetupHandler(timeout, isDemo, checkResponseSignature));
	}

	/// <summary>
	/// Check invoice
	/// </summary>
	/// <param name="invoice">Invoice to check</param>
	/// <param name="certificate">Signing certificate</param>
	/// <param name="timeout">Timeout in miliseconds, 0(default) = 100sec.</param>
	/// <param name="isDemo">Connect to production or demo CIS service</param>
	/// <param name="checkResponseSignature">Check if response signature is valid</param>
	/// <returns>Response from CIS service</returns>
	public ProvjeraOdgovor CheckInvoice(RacunType invoice, X509Certificate2 certificate,
		int timeout = 0, bool isDemo = false, bool checkResponseSignature = true)
	{
		// Send request
		return Fiscalization.CheckInvoice(invoice, certificate, ServiceSetupHandler(timeout, isDemo, checkResponseSignature));
	}

	/// <summary>
	/// Send echo request
	/// </summary>
	/// <param name="echo">String to send</param>
	/// <param name="timeout">Timeout in miliseconds, 0(default) = 100sec.</param>
	/// <param name="isDemo">Connect to production or demo CIS service</param>
	/// <returns>Response from CIS service</returns>
	public string SendEcho(string echo, int timeout = 0, bool isDemo = false)
	{
		// Send echo request
		return Fiscalization.SendEcho(echo, ServiceSetupHandler(timeout, isDemo, checkResponseSignature: false));
	}

	#endregion

	#region Helpers

	Action<FiskalizacijaService> ServiceSetupHandler(int timeout, bool isDemo, bool checkResponseSignature)
	{
		// FiskalizacijaService instance options
		return fs =>
		{
			// Set service URL
			// default = Fiscalization.SERVICE_URL_PRODUCTION
			if (isDemo)
			{
				fs.Url = Fiscalization.SERVICE_URL_DEMO;
			}

			// Set request timeout in miliseconds
			// default = 100s
			if (timeout != 0)
			{
				fs.Timeout = timeout;
			}

			// Response signature check
			fs.CheckResponseSignature = checkResponseSignature;

			// Logging
			fs.LogFileName = LogFileName;
		};
	}

	public RacunZahtjev CreateInvoiceRequest(RacunType invoice)
	{
		if (invoice == null)
			throw new ArgumentNullException("invoice");

		return new RacunZahtjev
		{
			Racun = invoice,
			Zaglavlje = Fiscalization.GetRequestHeader()
		};
	}

	public ProvjeraZahtjev CreateCheckRequest(RacunType invoice)
	{
		if (invoice == null)
			throw new ArgumentNullException("invoice");

		return new ProvjeraZahtjev
		{
			Racun = invoice,
			Zaglavlje = Fiscalization.GetRequestHeader()
		};
	}

	public void GenerateZki(RacunType invoice, X509Certificate2 certificate)
	{
		if (invoice == null)
			throw new ArgumentNullException("invoice");

		Fiscalization.GenerateZki(invoice, certificate);
	}

	public void Sign(ICisRequest request, X509Certificate2 certificate)
	{
		Fiscalization.Sign(request, certificate);
	}

	public string DateFormatLong(DateTime date)
	{
		return date.ToString(Fiscalization.DATE_FORMAT_LONG);
	}

	public string DateFormatShort(DateTime date)
	{
		return date.ToString(Fiscalization.DATE_FORMAT_SHORT);
	}

	public X509Certificate2 GetCertificateRaw(byte[] certRaw, string password = null)
	{
		if (certRaw == null)
			throw new ArgumentNullException("certRaw");

		return new X509Certificate2(certRaw, password);
	}

	public X509Certificate2 GetCertificateString(string certAsBase64EncodedString, string password = null)
	{
		if (certAsBase64EncodedString == null)
			throw new ArgumentNullException("certAsBase64EncodedString");

		var raw = Convert.FromBase64String(certAsBase64EncodedString);
		var cert = new X509Certificate2(raw, password);

		return cert;
	}

	public X509Certificate2 GetCertificateFile(string fileName, string password = null)
	{
		if (fileName == null)
			throw new ArgumentNullException("fileName");

		var raw = System.IO.File.ReadAllBytes(fileName);
		var cert = new X509Certificate2(raw, password);

		return cert;
	}

	public string ExportCertificate(string fileName, string password, string exportPassword)
	{
		if (fileName == null)
			throw new ArgumentNullException("fileName");

		var raw = System.IO.File.ReadAllBytes(fileName);
		var cert = new X509Certificate2(raw, password, X509KeyStorageFlags.Exportable);
		var rawExport = cert.Export(X509ContentType.Pkcs12, exportPassword);

		return Convert.ToBase64String(rawExport);
	}

	#region VBScript helpers - Variant() convert to type arrays

	public PorezType[] ToPorezTypeArray(object[] variantArray)
	{
		return variantArray.Cast<PorezType>().ToArray();
	}

	public PorezOstaloType[] ToPorezOstaloTypeArray(object[] variantArray)
	{
		return variantArray.Cast<PorezOstaloType>().ToArray();
	}

	public NaknadaType[] ToNaknadaTypeArray(object[] variantArray)
	{
		return variantArray.Cast<NaknadaType>().ToArray();
	}

	#endregion

	#endregion

	#region Enumerations

	#region OznakaSlijednostiType

	public OznakaSlijednostiType OznakaSlijednostiType_N
	{
		get { return OznakaSlijednostiType.N; }
	}

	public OznakaSlijednostiType OznakaSlijednostiType_P
	{
		get { return OznakaSlijednostiType.P; }
	}

	#endregion

	#region NacinPlacanjaType

	public NacinPlacanjaType NacinPlacanjaType_C
	{
		get { return NacinPlacanjaType.C; }
	}

	public NacinPlacanjaType NacinPlacanjaType_G
	{
		get { return NacinPlacanjaType.G; }
	}

	public NacinPlacanjaType NacinPlacanjaType_K
	{
		get { return NacinPlacanjaType.K; }
	}

	public NacinPlacanjaType NacinPlacanjaType_O
	{
		get { return NacinPlacanjaType.O; }
	}

	public NacinPlacanjaType NacinPlacanjaType_T
	{
		get { return NacinPlacanjaType.T; }
	}

	#endregion

	#endregion
}
