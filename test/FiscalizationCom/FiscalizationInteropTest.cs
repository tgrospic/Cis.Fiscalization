// Fiscalization API CIS 2012
// http://fiscalization.codeplex.com/
// Copyright (c) 2013 Tomislav Grospic

using Cis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Globalization;
using System.IO;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace FiscalizationTest
{
	[TestClass]
	public class FiscalizationInteropTest
	{
		[TestMethod]
		public void TestInvoiceRequest()
		{
			var certInfo = DemoCertificate.GetInfo();
			var com = new FiscalizationComInterop();
			var culture = CultureInfo.GetCultureInfo("en-GB");

			#region Build fiscalization request

			var invoice = new RacunType()
			{
				Oib = certInfo.Oib,
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

			var result = com.SendInvoiceRequest(request, certInfo.Certificate, 0, true);

			Assert.IsNotNull(result, "Result is null.");
			Assert.IsNotNull(result.Jir, "JIR is null.");
		}

		[TestMethod]
		public void TestLocationRequest()
		{
			var certInfo = DemoCertificate.GetInfo();
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
				Oib = certInfo.Oib,
				RadnoVrijeme = "radno vrijeme",
				DatumPocetkaPrimjene = DateTime.Now.AddDays(-60).ToString(Fiscalization.DATE_FORMAT_SHORT),
				SpecNamj = "112343454"
			};

			var request = com.CreateLocationRequest(loc);

			#endregion

			var result = com.SendLocationRequest(request, certInfo.Certificate, 0, true);

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
