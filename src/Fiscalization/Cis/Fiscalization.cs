// Fiscalization CIS 2012 by Tomislav Grospic
// http://fiscalization.codeplex.com/
using System;
using System.IO;
using System.Linq;
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

		public static RacunOdgovor SendInvoiceRequest(RacunZahtjev request, X509Certificate2 cert,
			Action<FiskalizacijaService> setupService = null)
		{
			if (request == null)
				throw new ArgumentNullException("request");
			if (request.Racun == null)
				throw new ArgumentNullException("request.Racun");

			return SignAndSendRequest<RacunZahtjev, RacunOdgovor>(request, x => x.racuni, cert, setupService);
		}

		public static PoslovniProstorOdgovor SendLocationRequest(PoslovniProstorZahtjev request, X509Certificate2 cert,
			Action<FiskalizacijaService> setupService = null)
		{
			if (request == null)
				throw new ArgumentNullException("request");
			if (request.PoslovniProstor == null)
				throw new ArgumentNullException("request.PoslovniProstor");

			return SignAndSendRequest<PoslovniProstorZahtjev, PoslovniProstorOdgovor>(
				request, x => x.poslovniProstor, cert, setupService);
		}

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

		public static TResult SignAndSendRequest<TSource, TResult>(TSource request,
			Func<FiskalizacijaService, Func<TSource, TResult>> serviceMethod, X509Certificate2 cert,
			Action<FiskalizacijaService> setupService = null)
			where TSource : IRequest
			where TResult : IResponse
		{
			if (request == null)
				throw new ArgumentNullException("request");
			if (serviceMethod == null)
				throw new ArgumentNullException("serviceMethod");
			if (cert == null)
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

		public static ZaglavljeType GetRequestHeader()
		{
			return new ZaglavljeType
			{
				IdPoruke = Guid.NewGuid().ToString(),
				DatumVrijeme = DateTime.Now.ToString(DATE_FORMAT_LONG)
			};
		}

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

		public static void Sign(IRequest request, X509Certificate2 certificate)
		{
			if (request == null)
				throw new ArgumentNullException("request");
			if (certificate == null)
				throw new ArgumentNullException("certificate");

			if (request.Signature != null)
				// Already signed
				return;

			// Check if ZKI is generated
			var invliceRequest = request as RacunZahtjev;
			if (invliceRequest != null && invliceRequest.Racun.ZastKod == null)
				invliceRequest.Racun.GenerateZki(certificate);

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

		public static bool CheckSignature(IRequest request)
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

		public static bool CheckSignatureXml(XmlDocument doc)
		{
			if (doc == null)
				throw new ArgumentNullException("doc");

			// Create new signed xml
			SignedXml signedXml = new SignedXml(doc);

			// Get signature xml node
			var signatureNode = doc.GetElementsByTagName("Signature")[0] as XmlElement;

			// Load signature node
			signedXml.LoadXml(signatureNode);

			// Get certificate
			X509Certificate2 certificate = null;
			foreach (KeyInfoX509Data x in signedXml.KeyInfo)
			{
				if (x.Certificates.Count > 0)
					certificate = (X509Certificate2)x.Certificates[0];
			}
			if (certificate == null)
				throw new ApplicationException("Can't find certificate in signature.");

			// Check signature
			return signedXml.CheckSignature(certificate, true);
		}

		public static string Serialize(IRequest request)
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

		#endregion
	}

	#region Interfaces

	public interface IRequest
	{
		string Id { get; set; }
		SignatureType Signature { get; set; }
	}

	public interface IResponse
	{
		GreskaType[] Greske { get; set; }
	}

	#endregion

	#region Partials

	public partial class FiskalizacijaService
	{
		#region Class

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
		}

		#endregion

		#region Members

		public bool CheckResponseSignature { get; set; }

		private SpyStream _writeStream = null;

		#endregion

		#region Overrides

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

		protected override XmlWriter GetWriterForMessage(System.Web.Services.Protocols.SoapClientMessage message, int bufferSize)
		{
			this._writeStream = new SpyStream(message.Stream);
			var wr = XmlWriter.Create(this._writeStream);

			return wr;
		}

		#endregion

		#region Logging

		partial void LogResponseRaw(XmlDocument request, XmlDocument response);

		#endregion
	}

	public partial class RacunZahtjev : IRequest { }

	public partial class PoslovniProstorZahtjev : IRequest { }

	public partial class PoslovniProstorOdgovor : IResponse { }

	public partial class RacunOdgovor : IResponse { }

	public partial class RacunType
	{
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
