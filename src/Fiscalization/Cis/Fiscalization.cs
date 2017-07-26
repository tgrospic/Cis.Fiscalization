// Cis.Fiscalization v1.3.0 :: CIS WSDL v1.4 (2012-2017)
// https://github.com/tgrospic/Cis.Fiscalization
// Copyright (c) 2013-present Tomislav Grospic
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Cis
{
	public static partial class Fiscalization
	{
		#region Constants

		public const string DATE_FORMAT_SHORT = "dd.MM.yyyy";
		public const string DATE_FORMAT_LONG = "dd.MM.yyyyTHH:mm:ss";

		public const string SERVICE_URL_PRODUCTION = "https://cis.porezna-uprava.hr:8449/FiskalizacijaService";
		public const string SERVICE_URL_DEMO = "https://cistest.apis-it.hr:8449/FiskalizacijaServiceTest";

		#endregion

		#region Service API

		/// <summary>
		/// Send invoice request
		/// </summary>
		/// <param name="request">Request to send</param>
		/// <param name="certificate">Signing certificate, optional if request is already signed</param>
		/// <param name="setupService">Function to set service settings</param>
		public static RacunOdgovor SendInvoiceRequest(RacunZahtjev request, X509Certificate2 certificate = null,
			Action<FiskalizacijaService> setupService = null)
		{
			if (request == null) throw new ArgumentNullException("request");
			if (request.Racun == null) throw new ArgumentNullException("request.Racun");

			return SignAndSendRequest<RacunZahtjev, RacunOdgovor>(request, x => x.racuni, certificate, setupService);
		}

		/// <summary>
		/// Send invoice
		/// </summary>
		/// <param name="invoice">Invoice to send</param>
		/// <param name="certificate">Signing certificate</param>
		/// <param name="setupService">Function to set service settings</param>
		public static RacunOdgovor SendInvoice(RacunType invoice, X509Certificate2 certificate,
			Action<FiskalizacijaService> setupService = null)
		{
			if (invoice == null) throw new ArgumentNullException("invoice");
			if (certificate == null) throw new ArgumentNullException("certificate");

			var request = new RacunZahtjev
			{
				Racun = invoice,
				Zaglavlje = Cis.Fiscalization.GetRequestHeader()
			};

			return SendInvoiceRequest(request, certificate, setupService);
		}

		/// <summary>
		/// Check invoice request
		/// </summary>
		/// <param name="request">Request to send</param>
		/// <param name="certificate">Signing certificate, optional if request is already signed</param>
		/// <param name="setupService">Function to set service settings</param>
		public static ProvjeraOdgovor CheckInvoiceRequest(ProvjeraZahtjev request, X509Certificate2 certificate = null,
			Action<FiskalizacijaService> setupService = null)
		{
			if (request == null) throw new ArgumentNullException("request");
			if (request.Racun == null) throw new ArgumentNullException("request.Racun");

			return SignAndSendRequest<ProvjeraZahtjev, ProvjeraOdgovor>(request, x => x.provjera, certificate, setupService);
		}

		/// <summary>
		/// Check invoice
		/// </summary>
		/// <param name="invoice">Invoice to check</param>
		/// <param name="certificate">Signing certificate</param>
		/// <param name="setupService">Function to set service settings</param>
		public static ProvjeraOdgovor CheckInvoice(RacunType invoice, X509Certificate2 certificate,
			Action<FiskalizacijaService> setupService = null)
		{
			if (invoice == null) throw new ArgumentNullException("invoice");
			if (certificate == null) throw new ArgumentNullException("certificate");

			var request = new ProvjeraZahtjev
			{
				Racun = invoice,
				Zaglavlje = Cis.Fiscalization.GetRequestHeader()
			};

			return CheckInvoiceRequest(request, certificate, setupService);
		}

		/// <summary>
		/// Send echo request
		/// </summary>
		/// <param name="echo">String to send</param>
		/// <param name="setupService">Function to set service settings</param>
		public static string SendEcho(string echo, Action<FiskalizacijaService> setupService = null)
		{
			if (echo == null) throw new ArgumentNullException("echo");

			// Create service endpoint
			var fs = new FiskalizacijaService();
			if (setupService != null)
				setupService(fs);

			// Response is not signed
			fs.CheckResponseSignature = false;

			// Send request
			return fs.echo(echo);
		}

		#endregion

		#region Send methods (generic)

		/// <summary>
		/// Send request
		/// </summary>
		/// <typeparam name="TRequest">Type of service method argument</typeparam>
		/// <typeparam name="TResponse">Type of service method result</typeparam>
		/// <param name="request">Request to send</param>
		/// <param name="serviceMethod">Function to provide service method</param>
		/// <param name="certificate">Signing certificate</param>
		/// <param name="setupService">Function to set service settings</param>
		/// <returns>Service response object</returns>
		public static TResponse SignAndSendRequest<TRequest, TResponse>(TRequest request,
			Func<FiskalizacijaService, Func<TRequest, TResponse>> serviceMethod, X509Certificate2 certificate,
			Action<FiskalizacijaService> setupService = null)
			where TRequest : ICisRequest
			where TResponse : ICisResponse
		{
			if (request == null) throw new ArgumentNullException("request");
			if (serviceMethod == null) throw new ArgumentNullException("serviceMethod");
			if (certificate == null && request.Signature == null) throw new ArgumentNullException("cert");

			// Create service endpoint
			var fs = new FiskalizacijaService();
			fs.CheckResponseSignature = true;
			if (setupService != null)
				setupService(fs);

			// Sign request
			Sign(request, certificate);

			// Send request to fiscalization service
			var method = serviceMethod(fs);
			var result = method(request);

			// Add reference to request object
			result.Request = request;

			ThrowOnResponseErrors(result);

			return result;
		}

		static void ThrowOnResponseErrors(ICisResponse response)
		{
			var errors = response?.Greske ?? new GreskaType[] { };

			// Special case for CheckInvoice service method
			// - returns error for success check WTF!!!!
			if ( response is ProvjeraOdgovor ) {
				// Remove "valid error" from response
				errors = errors.Where( x => x.SifraGreske != "v100" ).ToArray();
				response.Greske = errors;
			}

			if (errors.Any())
			{
				var strErrors = errors.Select(x => $"({x.SifraGreske}) {x.PorukaGreske}");
				var exMsg = string.Join("\n", strErrors);

				throw new Exception($"Fiscalization errors: {exMsg}");
			}
		}

		#endregion

		#region Helpers

		/// <summary>
		/// Sign request
		/// </summary>
		/// <param name="request">Request to sign</param>
		/// <param name="certificate">Signing certificate</param>
		public static void Sign(ICisRequest request, X509Certificate2 certificate)
		{
			if (request == null) throw new ArgumentNullException("request");

			if (request.Signature != null)
				// Already signed
				return;

			if (certificate == null) throw new ArgumentNullException("certificate");

			// Check if ZKI is generated
			var invoiceRequest = request as RacunZahtjev;
			if (invoiceRequest != null && invoiceRequest.Racun.ZastKod == null)
				GenerateZki(invoiceRequest.Racun, certificate);

			request.Id = request.GetType().Name;

			#region Sign request XML

			SignedXml xml = null;
			var ser = Serialize(request);
			var doc = new XmlDocument();
			doc.LoadXml(ser);

			xml = new SignedXml(doc);
			xml.SigningKey = certificate.PrivateKey;
			xml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;

			var keyInfo = new KeyInfo();
			var keyInfoData = new KeyInfoX509Data();
			keyInfoData.AddCertificate(certificate);
			keyInfoData.AddIssuerSerial(certificate.Issuer, certificate.GetSerialNumberString());
			keyInfo.AddClause(keyInfoData);
			xml.KeyInfo = keyInfo;

			var transforms = new Transform[]
			{
				new XmlDsigEnvelopedSignatureTransform(false),
				new XmlDsigExcC14NTransform(false)
			};

			Reference reference = new Reference("#" + request.Id);
			foreach (var x in transforms)
				reference.AddTransform(x);
			xml.AddReference(reference);
			xml.ComputeSignature();

			#endregion

			#region Fill request with signature data

			var s = xml.Signature;
			var certSerial = (X509IssuerSerial)keyInfoData.IssuerSerials[0];
			request.Signature = new SignatureType
			{
				SignedInfo = new SignedInfoType
				{
					CanonicalizationMethod = new CanonicalizationMethodType { Algorithm = s.SignedInfo.CanonicalizationMethod },
					SignatureMethod = new SignatureMethodType { Algorithm = s.SignedInfo.SignatureMethod },
					Reference =
						(from x in s.SignedInfo.References.OfType<Reference>()
						 select new ReferenceType
						 {
							 URI = x.Uri,
							 Transforms =
								 (from t in transforms
								  select new TransformType { Algorithm = t.Algorithm }).ToArray(),
							 DigestMethod = new DigestMethodType { Algorithm = x.DigestMethod },
							 DigestValue = x.DigestValue
						 }).ToArray()
				},
				SignatureValue = new SignatureValueType { Value = s.SignatureValue },
				KeyInfo = new KeyInfoType
				{
					ItemsElementName = new[] { ItemsChoiceType2.X509Data },
					Items = new[]
					{
						new X509DataType
						{
							ItemsElementName = new[]
							{
								ItemsChoiceType.X509IssuerSerial,
								ItemsChoiceType.X509Certificate
							},
							Items = new object[]
							{
								new X509IssuerSerialType
								{
									X509IssuerName = certSerial.IssuerName,
									X509SerialNumber = certSerial.SerialNumber
								},
								certificate.RawData
							}
						}
					}
				}
			};

			#endregion
		}

		/// <summary>
		/// Generate ZKI code
		/// </summary>
		/// <param name="invoice">Invoice to calculate and generate ZKI</param>
		/// <param name="certificate">Signing certificate</param>
		public static void GenerateZki(RacunType invoice, X509Certificate2 certificate)
		{
			if (certificate == null) throw new ArgumentNullException("certificate");

			StringBuilder sb = new StringBuilder();
			sb.Append(invoice.Oib);
			sb.Append(invoice.DatVrijeme);
			sb.Append(invoice.BrRac.BrOznRac);
			sb.Append(invoice.BrRac.OznPosPr);
			sb.Append(invoice.BrRac.OznNapUr);
			sb.Append(invoice.IznosUkupno);

			invoice.ZastKod = Fiscalization.SignAndHashMD5(sb.ToString(), certificate);
		}

		/// <summary>
		/// Sign and hash with MD5 algorithm
		/// </summary>
		/// <param name="value">String to encrypt</param>
		/// <param name="certificate">Signing certificate</param>
		/// <returns>Encrypted string</returns>
		public static string SignAndHashMD5(string value, X509Certificate2 certificate)
		{
			if (value == null) throw new ArgumentNullException("value");
			if (certificate == null) throw new ArgumentNullException("certificate");

			// Sign data
			byte[] b = Encoding.ASCII.GetBytes(value);
			RSACryptoServiceProvider provider = (RSACryptoServiceProvider)certificate.PrivateKey;
			var signData = provider.SignData(b, new SHA1CryptoServiceProvider());

			// Compute hash
			MD5 md5 = MD5.Create();
			byte[] hash = md5.ComputeHash(signData);
			var result = new string(hash.SelectMany(x => x.ToString("x2")).ToArray());

			return result;
		}

		/// <summary>
		/// Check signature on request object
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		public static bool CheckSignature(ICisRequest request)
		{
			if (request == null) throw new ArgumentNullException("response");
			if (request.Signature == null) throw new ArgumentNullException("Document not signed.");

			// Load signed XML
			var doc = new XmlDocument();
			var ser = Serialize(request);
			doc.LoadXml(ser);

			// Check signature
			return CheckSignatureXml(doc);
		}

		/// <summary>
		/// Check signature on signed XML document
		/// </summary>
		/// <param name="doc">Signed XML document</param>
		/// <returns></returns>
		public static bool CheckSignatureXml(XmlDocument doc)
		{
			if (doc == null) throw new ArgumentNullException("doc");

			// Get signature property name with lambda expression
			Expression<Func<ICisRequest, SignatureType>> selector = x => x.Signature;
			var signatureNodeName = (selector.Body as MemberExpression).Member.Name;

			// Get signature xml node
			var signatureNode = doc.GetElementsByTagName(signatureNodeName)[0] as XmlElement;

			// Signed xml (RacunOdgovor) inside SOAP XML document
			SignedXml signedXml = new SignedXml((XmlElement)doc.DocumentElement.FirstChild.FirstChild);

			// Load signature node
			signedXml.LoadXml(signatureNode);

			// Check signature
			return signedXml.CheckSignature();
		}

		/// <summary>
		/// Serialize request data
		/// </summary>
		/// <param name="request">Request to serialize</param>
		/// <returns></returns>
		public static string Serialize(ICisRequest request)
		{
			if (request == null) throw new ArgumentNullException("request");

			// Fix empty arrays to null
			if (request is RacunZahtjev)
			{
				var rz = (RacunZahtjev)request;

				if (rz.Racun == null) throw new ArgumentNullException("request.Racun");

				var r = rz.Racun;
				Action<Array, Action> fixArray = (x, y) =>
				{
					var isEmpty = x != null && !x.OfType<object>().Any(x1 => x1 != null);
					if (isEmpty)
						y();
				};
				fixArray(r.Naknade, () => r.Naknade = null);
				fixArray(r.OstaliPor, () => r.OstaliPor = null);
				fixArray(r.Pdv, () => r.Pdv = null);
				fixArray(r.Pnp, () => r.Pnp = null);
			}

			using (var ms = new MemoryStream())
			{
				// Set namespace to root element
				var root = new XmlRootAttribute { Namespace = "http://www.apis-it.hr/fin/2012/types/f73", IsNullable = false };
				var ser = new XmlSerializer(request.GetType(), root);
				ser.Serialize(ms, request);

				return Encoding.UTF8.GetString(ms.ToArray());
			}
		}

		/// <summary>
		/// Get default request header
		/// </summary>
		/// <returns></returns>
		public static ZaglavljeType GetRequestHeader()
		{
			return new ZaglavljeType
			{
				IdPoruke = Guid.NewGuid().ToString(),
				DatumVrijeme = DateTime.Now.ToString(DATE_FORMAT_LONG)
			};
		}

		#endregion
	}

	#region Interfaces

	/// <summary>
	/// Represent request data for CIS service
	/// </summary>
	public interface ICisRequest
	{
		string Id { get; set; }
		SignatureType Signature { get; set; }
	}

	/// <summary>
	/// Represent response data from CIS service
	/// </summary>
	public interface ICisResponse
	{
		GreskaType[] Greske { get; set; }
		ICisRequest Request { get; set; }
	}

	#endregion

	#region FiskalizacijaService partial implementation

	public partial class FiskalizacijaService
	{
		#region Fields

		public bool CheckResponseSignature { get; set; }

		SpyStream _writeStream;

		#endregion

		#region SOAP interceptor, logging

		partial void LogResponseRaw(XmlDocument request, XmlDocument response);

		/// <summary>
		/// Intercept request messages
		/// </summary>
		/// <param name="message"></param>
		/// <param name="bufferSize"></param>
		/// <returns></returns>
		protected override XmlWriter GetWriterForMessage(System.Web.Services.Protocols.SoapClientMessage message, int bufferSize)
		{
			_writeStream = new SpyStream(message.Stream);
			var wr = XmlWriter.Create(_writeStream);

			return wr;
		}

		/// <summary>
		/// Intercept response messages
		/// </summary>
		/// <param name="message"></param>
		/// <param name="bufferSize"></param>
		/// <returns></returns>
		protected override XmlReader GetReaderForMessage(System.Web.Services.Protocols.SoapClientMessage message, int bufferSize)
		{
			// Load response XML
			var reader = base.GetReaderForMessage(message, bufferSize);
			var docResponse = new XmlDocument();
			docResponse.PreserveWhitespace = true;
			docResponse.Load(reader);

			// Check signature
			if (CheckResponseSignature)
			{
				var isValid = Fiscalization.CheckSignatureXml(docResponse);
				if (!isValid)
					throw new ApplicationException("Soap response signature not valid.");
			}

			// Read request XML
			var docRequest = new XmlDocument();
			docRequest.PreserveWhitespace = true;
			_writeStream.Seek(0, SeekOrigin.Begin);
			docRequest.Load(_writeStream);

			// Log response
			LogResponseRaw(docRequest, docResponse);

			return XmlReader.Create(new StringReader(docResponse.InnerXml));
		}

		#region SpyStream

		/// <summary>
		/// Custom stream to monitor other writeable stream.
		/// Depends on Flush method
		/// </summary>
		class SpyStream : MemoryStream
		{
			Stream _writeStream = null;
			long _lastPosition = 0;

			public SpyStream(Stream writeStream)
			{
				_writeStream = writeStream;
			}

			public override void Flush()
			{
				Seek(_lastPosition, SeekOrigin.Begin);

				// Write to underlying stream
				CopyTo_(_writeStream);

				_lastPosition = Position;
			}

			public override void Close()
			{
				base.Close();

				_writeStream.Close();
			}

			// If isn't .NET4
			void CopyTo_(Stream destination, int bufferSize = 4096)
			{
				byte[] buffer = new byte[bufferSize];
				int read;
				while ((read = Read(buffer, 0, buffer.Length)) != 0)
					destination.Write(buffer, 0, read);
			}
		}

		#endregion

		#endregion
	}

	public partial class RacunZahtjev : ICisRequest { }

	public partial class RacunOdgovor : ICisResponse
	{
		[XmlIgnore]
		public ICisRequest Request { get; set; }
	}

	public partial class ProvjeraZahtjev : ICisRequest { }

	public partial class ProvjeraOdgovor : ICisResponse
	{
		[XmlIgnore]
		public ICisRequest Request { get; set; }
	}

	public partial class RacunType
	{
		/// <summary>
		/// Generate ZKI code
		/// </summary>
		/// <param name="certificate">Signing certificate</param>
		[Obsolete("Use static method Fiscalization.GenerateZki instead.")]
		public void GenerateZki(X509Certificate2 certificate)
		{
			Fiscalization.GenerateZki(this, certificate);
		}
	}

	#endregion
}
