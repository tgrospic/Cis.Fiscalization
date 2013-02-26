// Fiscalization API CIS 2012
// http://fiscalization.codeplex.com/
// Copyright (c) 2013 Tomislav Grospic

using Cis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Globalization;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace FiscalizationTest
{
	[TestClass]
	public class FiscalizationInteropTest
	{
		#region Constants

		// DEMO certificate and OIB not included in project source code
		// You can paste your certificate and OIB
		// or/and change GetCertificate method
		const string DEMO_CERTIFICATE = DemoCertificate.CERT;
		const string DEMO_OIB = DemoCertificate.OIB;

		X509Certificate2 GetCertificate()
		{
			var certRaw = Convert.FromBase64String(DEMO_CERTIFICATE);
			var certificate = new X509Certificate2(certRaw);

			return certificate;
		}

		#endregion

		[TestMethod]
		public void TestInvoiceRequest()
		{
			var com = new FiscalizationComInterop();
			var culture = CultureInfo.GetCultureInfo("en-GB");

			#region Build fiscalization request

			var invoice = new RacunType()
			{
				Oib = DEMO_OIB,
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
				}
			};

			var request = com.CreateInvoiceRequest(invoice);

			#endregion

			var result = com.SendInvoiceRequest(request, DEMO_CERTIFICATE, 0, true);

			Assert.IsNotNull(result, "Result is null.");
			Assert.IsNotNull(result.Jir, "JIR is null.");
		}

		[TestMethod]
		public void TestLocationRequest()
		{
			var com = new FiscalizationComInterop();
			var culture = CultureInfo.GetCultureInfo("en-GB");

			#region Build fiscalization request

			var loc = new PoslovniProstorType()
			{
				AdresniPodatak = new AdresniPodatakType
				{
					Item = new AdresaType()
					{
						Ulica = "Ulica",
						BrojPoste = "10000",
						Naselje = "Naselje",
						Opcina = "Opcina",
					}
				},
				OznPoslProstora = "1",
				Oib = DEMO_OIB,
				RadnoVrijeme = "radno vrijeme",
				DatumPocetkaPrimjene = DateTime.Now.AddDays(-60).ToString(Fiscalization.DATE_FORMAT_SHORT),
				SpecNamj = "112343454"
			};

			var request = com.CreateLocationRequest(loc);

			#endregion

			var result = com.SendLocationRequest(request, DEMO_CERTIFICATE, 0, true);

			Assert.IsNotNull(result, "Result is null.");
		}

		[TestMethod]
		public void TestEchoRequest()
		{
			var com = new FiscalizationComInterop();
			var msg = "echo message";
			var result = com.SendEcho(msg, 0, true);

			Assert.AreEqual(msg, result, "Echo method result not equal.");
		}
	}
}
