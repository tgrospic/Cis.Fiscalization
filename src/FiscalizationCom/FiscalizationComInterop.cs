// Fiscalization API CIS 2012 v1.0.0
// http://fiscalization.codeplex.com/
// Copyright (c) 2013 Tomislav Grospic
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using Cis;
using Helpers;
using System;

public class FiscalizationComInterop
{
	#region Service methods

	/// <summary>
	/// Send invoice request
	/// </summary>
	/// <param name="request">Request to send</param>
	/// <param name="certAsBase64EncodedString">Signing certificate as base64 encoded string</param>
	/// <param name="timeout">Timeout in miliseconds, 0(default) = 100sec.</param>
	/// <param name="isDemo">Connect to production or demo CIS service</param>
	/// <returns>Response from CIS service</returns>
	public RacunOdgovor SendInvoiceRequest(RacunZahtjev request, string certAsBase64EncodedString, int timeout = 0, bool isDemo = false)
	{
		X509Certificate2Wrapper cert = certAsBase64EncodedString;

		// Send request
		var result = Fiscalization.SendInvoiceRequest(request, cert,
			x =>
			{
				// Change service URL
				// default = Fiscalization.SERVICE_URL_PRODUCTION
				if (isDemo)
				{
					x.Url = Fiscalization.SERVICE_URL_DEMO;
				}

				// Set request timeout in miliseconds
				// default = 100s
				if (timeout != 0)
				{
					x.Timeout = timeout;
				}

				// We can disable response signature checking
				// default = true
				// x.CheckResponseSignature = false;
			});

		return result;
	}

	/// <summary>
	/// Send location request
	/// </summary>
	/// <param name="request">Request to send</param>
	/// <param name="certAsBase64EncodedString">Signing certificate as base64 encoded string</param>
	/// <param name="timeout">Timeout in miliseconds, 0(default) = 100sec.</param>
	/// <param name="isDemo">Connect to production or demo CIS service</param>
	/// <returns>Response from CIS service</returns>
	public PoslovniProstorOdgovor SendLocationRequest(PoslovniProstorZahtjev request, string certAsBase64EncodedString, int timeout = 0, bool isDemo = false)
	{
		X509Certificate2Wrapper cert = certAsBase64EncodedString;

		// Send request
		var result = Fiscalization.SendLocationRequest(request, cert,
			x =>
			{
				// Change service URL
				// default = Fiscalization.SERVICE_URL_PRODUCTION
				if (isDemo)
				{
					x.Url = Fiscalization.SERVICE_URL_DEMO;
				}

				// Set request timeout in miliseconds
				// default = 100s
				if (timeout != 0)
				{
					x.Timeout = timeout;
				}

				// We can disable response signature checking
				// default = true
				// x.CheckResponseSignature = false;
			});

		return result;
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
		var result = Fiscalization.SendEcho(echo,
			x =>
			{
				// Change service URL
				// default = Fiscalization.SERVICE_URL_PRODUCTION
				if (isDemo)
				{
					x.Url = Fiscalization.SERVICE_URL_DEMO;
				}

				// Set request timeout in miliseconds
				// default = 100s
				if (timeout != 0)
				{
					x.Timeout = timeout;
				}
			});

		return result;
	}

	#endregion

	#region Helpers

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

	public PoslovniProstorZahtjev CreateLocationRequest(PoslovniProstorType location)
	{
		if (location == null)
			throw new ArgumentNullException("location");

		return new PoslovniProstorZahtjev
		{
			PoslovniProstor = location,
			Zaglavlje = Fiscalization.GetRequestHeader()
		};
	}

	public void GenerateZki(RacunType invoice, string certAsBase64EncodedString)
	{
		if (invoice == null)
			throw new ArgumentNullException("invoice");

		if (certAsBase64EncodedString == null)
			throw new ArgumentNullException("certAsBase64EncodedString");

		X509Certificate2Wrapper cert = certAsBase64EncodedString;

		invoice.GenerateZki(cert);
	}

	public void Sign(ICisRequest request, string certAsBase64EncodedString)
	{
		if (request == null)
			throw new ArgumentNullException("request");

		if (certAsBase64EncodedString == null)
			throw new ArgumentNullException("certAsBase64EncodedString");

		X509Certificate2Wrapper cert = certAsBase64EncodedString;

		Fiscalization.Sign(request, cert);
	}

	public string DateFormatLong(DateTime date)
	{
		return date.ToString(Fiscalization.DATE_FORMAT_LONG);
	}

	public string DateFormatShort(DateTime date)
	{
		return date.ToString(Fiscalization.DATE_FORMAT_SHORT);
	}

	#endregion
}
