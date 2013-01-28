// Fiscalization CIS 2012 by Tomislav Grospic
// http://fiscalization.codeplex.com/

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using Cis;
using System.Security.Authentication;

namespace FiscalizationTest
{
	[TestClass]
	public class FiscalizationTest
	{
		[TestMethod]
		public void TestInvoiceRequest()
		{
			// Certificate
			var certRaw = Convert.FromBase64String(DemoCertificate.CERT);
			var certificate = new X509Certificate2(certRaw);
			var culture = CultureInfo.GetCultureInfo("en-GB");

			#region Build fiscalization request

			var invoice = new RacunType()
			{
				Oib = DemoCertificate.OIB,
				USustPdv = true,
				DatVrijeme = DateTime.Now.ToString(Fiscalization.DATE_FORMAT_LONG),
				OznSlijed = OznakaSlijednostiType.N,
				IznosUkupno = 3.ToString("N2", culture),
				NacinPlac = NacinPlacanjaType.G,
				OibOper = "98642375382",
				NakDost = false,
				BrRac = new BrojRacunaType()
				{
					BrOznRac = "1",
					OznPosPr = "1",
					OznNapUr = "1"
				},
				Pdv = new[]
				{
					new PorezType
					{
						Stopa = 25M.ToString("N2", culture),
						Osnovica = 2.34M.ToString("N2", culture),
						Iznos = 0.59M.ToString("N2", culture),
					}
				}
			};

			var request = new RacunZahtjev
			{
				Racun = invoice,
				Zaglavlje = Cis.Fiscalization.GetRequestHeader()
			};

			#endregion

			// Send request
			// Response signature is checked automaticaly
			var result = Fiscalization.SendInvoiceRequest(request, certificate,
				x =>
				{
					// SOAP service settings
					// Change service URL
					// default = Fiscalization.SERVICE_URL_PRODUCTION
					x.Url = Fiscalization.SERVICE_URL_DEMO;

					// Set request timeout in miliseconds
					// default = 100s
					x.Timeout = 2000;

					// We can disable response signature checking
					// x.CheckResponseSignature = false;
				});

			// Check request signature
			var isValid = Fiscalization.CheckSignature(request);
			
			Assert.IsTrue(isValid, "Request signature check failed.");
			Assert.IsNotNull(result, "Result is null.");
			Assert.IsNotNull(result.Jir, "JIR is null.");
		}

		[TestMethod]
		[ExpectedException(typeof(ApplicationException), "Wrong data send to CIS service")]
		public void TestLocationRequest()
		{
			// Certificate
			var certRaw = Convert.FromBase64String(DemoCertificate.CERT);
			var certificate = new X509Certificate2(certRaw);
			var culture = CultureInfo.GetCultureInfo("en-GB");

			#region Build fiscalization request

			var loc = new PoslovniProstorType()
			{
				AdresniPodatak = new AdresniPodatakType
				{
					Item = new AdresaType()
					{
						Ulica = "Ulica",
						BrojPoste = "BrojPoste",
						Naselje = "Naselje",
						Opcina = "Opcina",
					}
				},
				Oib = DemoCertificate.OIB,
				RadnoVrijeme = "radno vrijeme",
				DatumPocetkaPrimjene = DateTime.Now.AddDays(-60).ToString(Fiscalization.DATE_FORMAT_SHORT),
				SpecNamj = "112343454"
			};

			var request = new PoslovniProstorZahtjev
			{
				PoslovniProstor = loc,
				Zaglavlje = Cis.Fiscalization.GetRequestHeader()
			};

			#endregion

			// Send request
			// Response signature is checked automaticaly
			var result = Fiscalization.SendLocationRequest(request, certificate);

			// Check request signature
			var isValid = Fiscalization.CheckSignature(request);

			Assert.IsTrue(isValid, "Request signature check failed.");
			Assert.IsNotNull(result, "Result is null.");
		}


		[TestMethod]
		public void TestEchoRequest()
		{
			var msg = "echo message";
			var result = Fiscalization.SendEcho(msg, x => x.Url = Fiscalization.SERVICE_URL_DEMO);

			Assert.AreEqual(msg, result, "Echo method result not equal.");
		}
	}
}
