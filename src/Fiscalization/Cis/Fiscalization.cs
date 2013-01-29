// Fiscalization API CIS 2012 v1.0.0
// http://fiscalization.codeplex.com/
// Copyright (c) 2013 Tomislav Grospic
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
	public static class Fiscalization
	{
		#region Constants

		public const string DATE_FORMAT_SHORT = "dd.MM.yyyy";
		public const string DATE_FORMAT_LONG = "dd.MM.yyyyTHH:mm:ss";

		public const string SERVICE_URL_PRODUCTION = "https://cis.porezna-uprava.hr:8449/FiskalizacijaService";
		public const string SERVICE_URL_DEMO = "https://cistest.apis-it.hr:8449/FiskalizacijaServiceTest";

		#endregion

		#region Service methods

		/// <summary>
		/// Send invoice request
		/// </summary>
		/// <param name="request">Request to send</param>
		/// <param name="cert">Signing certificate</param>
		/// <param name="setupService">Function to set service settings</param>
		/// <returns></returns>
		public static RacunOdgovor SendInvoiceRequest(RacunZahtjev request, X509Certificate2 cert = null,
			Action<FiskalizacijaService> setupService = null)
		{
			if (request == null)
				throw new ArgumentNullException("request");
			if (request.Racun == null)
				throw new ArgumentNullException("request.Racun");

			return SignAndSendRequest<RacunZahtjev, RacunOdgovor>(request, x => x.racuni, cert, setupService);
		}

		/// <summary>
		/// Send location request
		/// </summary>
		/// <param name="request">Request to send</param>
		/// <param name="cert">Signing certificate</param>
		/// <param name="setupService">Function to set service settings</param>
		/// <returns></returns>
		public static PoslovniProstorOdgovor SendLocationRequest(PoslovniProstorZahtjev request, X509Certificate2 cert = null,
			Action<FiskalizacijaService> setupService = null)
		{
			if (request == null)
				throw new ArgumentNullException("request");
			if (request.PoslovniProstor == null)
				throw new ArgumentNullException("request.PoslovniProstor");

			return SignAndSendRequest<PoslovniProstorZahtjev, PoslovniProstorOdgovor>(
				request, x => x.poslovniProstor, cert, setupService);
		}

		/// <summary>
		/// Send echo request
		/// </summary>
		/// <param name="echo">String to send</param>
		/// <param name="setupService">Function to set service settings</param>
		/// <returns></returns>
		public static string SendEcho(string echo, Action<FiskalizacijaService> setupService = null)
		{
			if (echo == null)
				throw new ArgumentNullException("echo");

			// Create service endpoint
			var fs = new FiskalizacijaService();
			if (setupService != null)
				setupService(fs);

			// Send request
			var result = fs.echo(echo);

			return result;
		}

		/// <summary>
		/// Send request
		/// </summary>
		/// <typeparam name="TSource">Type of service method argument</typeparam>
		/// <typeparam name="TResult">Type of service method result</typeparam>
		/// <param name="request">Request to send</param>
		/// <param name="serviceMethod">Function to provide service method</param>
		/// <param name="cert">Signing certificate</param>
		/// <param name="setupService">Function to set service settings</param>
		/// <returns></returns>
		public static TResult SignAndSendRequest<TSource, TResult>(TSource request,
			Func<FiskalizacijaService, Func<TSource, TResult>> serviceMethod, X509Certificate2 cert = null,
			Action<FiskalizacijaService> setupService = null)
			where TSource : ICisRequest
			where TResult : ICisResponse
		{
			if (request == null)
				throw new ArgumentNullException("request");
			if (serviceMethod == null)
				throw new ArgumentNullException("serviceMethod");
			if (cert == null && request.Signature == null)
				throw new ArgumentNullException("cert");

			// Create service endpoint
			var fs = new FiskalizacijaService();
			fs.CheckResponseSignature = true;
			if (setupService != null)
				setupService(fs);

			// Sign request
			Sign(request, cert);

			// Send request to fiscalization service
			var method = serviceMethod(fs);
			var result = method(request);
			if (result != null && result.Greske != null)
			{
				var sb = new StringBuilder();
				foreach (var x in result.Greske)
				{
					sb.AppendLine();
					sb.AppendFormat("({0}) {1}", x.SifraGreske, x.PorukaGreske);
				}
				var exMsg = string.Format("Fiscalization errors: {0}", sb);
				throw new ApplicationException(exMsg);
			}
			return result;
		}

		#endregion

		#region Helpers

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

		/// <summary>
		/// Sign and hash with MD5 algorithm
		/// </summary>
		/// <param name="value">String to encrypt</param>
		/// <param name="certificate">Signing certificate</param>
		/// <returns>Encrypted string</returns>
		public static string SignAndHashMD5(string value, X509Certificate2 certificate)
		{
			if (value == null)
				throw new ArgumentNullException("value");
			if (certificate == null)
				throw new ArgumentNullException("certificate");

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
		/// Sign request
		/// </summary>
		/// <param name="request">Request to sign</param>
		/// <param name="certificate">Signing certificate</param>
		public static void Sign(ICisRequest request, X509Certificate2 certificate)
		{
			if (request == null)
				throw new ArgumentNullException("request");

			if (request.Signature != null)
				// Already signed
				return;

			if (certificate == null)
				throw new ArgumentNullException("certificate");

			// Check if ZKI is generated
			var invoiceRequest = request as RacunZahtjev;
			if (invoiceRequest != null && invoiceRequest.Racun.ZastKod == null)
				invoiceRequest.Racun.GenerateZki(certificate);

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

			#region Create signature data

			// Create signature
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
		/// Check signature on request object
		/// </summary>
		/// <param name="request"></param>
		/// <returns></returns>
		public static bool CheckSignature(ICisRequest request)
		{
			if (request == null)
				throw new ArgumentNullException("response");
			if (request.Signature == null)
				throw new ArgumentNullException("Document not signed.");

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
			if (doc == null)
				throw new ArgumentNullException("doc");

			// Create new signed xml
			SignedXml signedXml = new SignedXml(doc);

			// Get signature property name by lambda expression
			var signatureNodeName = GetPropertyName(x => x.Signature);

			// Get signature xml node
			var signatureNode = doc.GetElementsByTagName(signatureNodeName)[0] as XmlElement;

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
			if (request == null)
				throw new ArgumentNullException("request");

			// Fix empty arrays to null
			if (request is RacunZahtjev)
			{
				var rz = (RacunZahtjev)request;

				if (rz.Racun == null)
					throw new ArgumentNullException("request.Racun");

				var r = rz.Racun;
				Action<Array, Action> fixArray = (x, y) =>
				{
					if (x != null && x.Length == 0)
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
		/// Get property name by lambda expression
		/// </summary>
		/// <param name="selector">Function to select property</param>
		/// <returns></returns>
		private static string GetPropertyName(Expression<Func<ICisRequest, SignatureType>> selector)
		{
			var property = selector.Body as MemberExpression;

			return property.Member.Name;
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
	}

	#endregion

	#region Partials

	public partial class FiskalizacijaService
	{
		#region Class

		/// <summary>
		/// Custom stream to monitor other writeable stream.
		/// Depends on Flush method
		/// </summary>
		private class SpyStream : MemoryStream
		{
			Stream writeStream = null;
			long lastPosition = 0;

			public SpyStream(Stream writeStream)
			{
				this.writeStream = writeStream;
			}

			public override void Flush()
			{
				var position = this.Position;

				// Write to underlying stream
				this.Seek(lastPosition, SeekOrigin.Begin);
				var br = new BinaryReader(this);

				var count = position - lastPosition;
				var result = br.ReadBytes((int)count);
				writeStream.Write(result, 0, (int)count);

				lastPosition = this.Position;

				base.Flush();
			}

			public override void Close()
			{
				base.Close();

				this.writeStream.Close();
			}
		}

		#endregion

		#region Members

		public bool CheckResponseSignature { get; set; }

		private SpyStream _writeStream = null;

		#endregion

		#region Overrides

		/// <summary>
		/// Intercept request messages
		/// </summary>
		/// <param name="message"></param>
		/// <param name="bufferSize"></param>
		/// <returns></returns>
		protected override XmlWriter GetWriterForMessage(System.Web.Services.Protocols.SoapClientMessage message, int bufferSize)
		{
			this._writeStream = new SpyStream(message.Stream);
			var wr = XmlWriter.Create(this._writeStream);

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
			if (this.CheckResponseSignature)
			{
				var isValid = Fiscalization.CheckSignatureXml(docResponse);
				if (!isValid)
					throw new ApplicationException("Soap response signature not valid.");
			}

			// Read request XML
			var docRequest = new XmlDocument();
			docRequest.PreserveWhitespace = true;
			this._writeStream.Seek(0, SeekOrigin.Begin);
			docRequest.Load(this._writeStream);

			// Log response
			this.LogResponseRaw(docRequest, docResponse);

			return System.Xml.XmlReader.Create(new StringReader(docResponse.InnerXml));
		}

		#endregion

		#region Logging

		partial void LogResponseRaw(XmlDocument request, XmlDocument response);

		#endregion
	}

	public partial class RacunZahtjev : ICisRequest { }

	public partial class PoslovniProstorZahtjev : ICisRequest { }

	public partial class PoslovniProstorOdgovor : ICisResponse { }

	public partial class RacunOdgovor : ICisResponse { }

	public partial class RacunType
	{
		/// <summary>
		/// Generate ZKI code
		/// </summary>
		/// <param name="certificate">Signing certificate</param>
		public void GenerateZki(X509Certificate2 certificate)
		{
			if (certificate == null)
				throw new ArgumentNullException("certificate");

			StringBuilder sb = new StringBuilder();
			sb.Append(this.Oib);
			sb.Append(this.DatVrijeme);
			sb.Append(this.BrRac.BrOznRac);
			sb.Append(this.BrRac.OznPosPr);
			sb.Append(this.BrRac.OznNapUr);
			sb.Append(this.IznosUkupno);

			this.ZastKod = Fiscalization.SignAndHashMD5(sb.ToString(), certificate);
		}
	}

	#endregion
}
