using Cis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace FiscalizationTest
{
	[TestClass]
	public class FiscalizationTest
	{
		TestEnvironment Demo = TestEnvironment.Create();

		void DemoSetup(FiskalizacijaService fs)
		{
			// Set CIS service URL
			// default = Fiscalization.SERVICE_URL_PRODUCTION
			fs.Url = Fiscalization.SERVICE_URL_DEMO;

			// Set request timeout in miliseconds
			// default = 100s
			fs.Timeout = 2000;

			// Set response signature checking
			// default = true
			fs.CheckResponseSignature = true;
		}

		// Test async API
		[TestMethod]
		public async Task TestSendInvoiceAsync()
		{
			// Create demo invoice
			RacunType invoice = Demo.Invoice(Demo.Oib);

			var result = await Fiscalization.SendInvoiceAsync(invoice, Demo.Certificate, DemoSetup);

			Assert.IsNotNull(result, "Result is null.");
			Assert.IsNotNull(result.Jir, "JIR is null.");
		}

		[TestMethod]
		[ExpectedException(typeof(Exception), "Invalid OIB sent to CIS service.")]
		public async Task TestSendInvoiceAsyncInvalid()
		{
			// Create demo invoice with invalid OIB, must throw exception (message from GreskaType)
			RacunType invoice = Demo.Invoice("invalid OIB");

			await Fiscalization.SendInvoiceAsync(invoice, Demo.Certificate, DemoSetup);
		}

		[TestMethod]
		public async Task TestCheckInvoiceAsync()
		{
			// Create demo invoice
			RacunType invoice = Demo.Invoice(Demo.Oib);

			// Send demo invoice, fill with JIR
			var _ = await Fiscalization.SendInvoiceAsync(invoice, Demo.Certificate, DemoSetup);

			var result = await Fiscalization.CheckInvoiceAsync(invoice, Demo.Certificate, DemoSetup);

			Assert.IsNotNull(result, "Result is null.");
		}

		[TestMethod]
		[ExpectedException(typeof(Exception), "Check invalid invoice.")]
		public async Task TestCheckInvoiceAsyncInvalid()
		{
			// Create demo invoice
			RacunType invoice = Demo.Invoice(Demo.Oib);

			var result = await Fiscalization.CheckInvoiceAsync(invoice, Demo.Certificate, DemoSetup);
		}

		[TestMethod]
		public async Task TestSendEchoAsync()
		{
			var msg = "echo message";
			var result = await Fiscalization.SendEchoAsync(msg, DemoSetup);

			Assert.AreEqual(msg, result, "Echo method result not equal.");
		}

		// Test sync API
		[TestMethod]
		public void TestSendInvoice()
		{
			// Create demo invoice
			RacunType invoice = Demo.Invoice(Demo.Oib);

			// Send request with response signature checking
			var result = Fiscalization.SendInvoice(invoice, Demo.Certificate, DemoSetup);

			Assert.IsNotNull(result, "Result is null.");
			Assert.IsNotNull(result.Jir, "JIR is null.");
		}

		[TestMethod]
		[ExpectedException(typeof(Exception), "Invalid OIB sent to CIS service.")]
		public void TestSendInvoiceInvalid()
		{
			// Create demo invoice with invalid OIB, must throw exception (message from GreskaType)
			RacunType invoice = Demo.Invoice("invalid OIB");

			Fiscalization.SendInvoice(invoice, Demo.Certificate, DemoSetup);
		}

		[TestMethod]
		public void TestCheckInvoice()
		{
			// Create demo invoice
			RacunType invoice = Demo.Invoice(Demo.Oib);

			// Send demo invoice, fill with JIR
			var _ = Fiscalization.SendInvoice(invoice, Demo.Certificate, DemoSetup);

			// Send invoice for checking
			var result = Fiscalization.CheckInvoice(invoice, Demo.Certificate, DemoSetup);

			Assert.IsNotNull(result, "Result is null.");
		}

		[TestMethod]
		[ExpectedException(typeof(Exception), "Check invalid invoice.")]
		public void TestCheckInvoiceInvalid()
		{
			// Create demo invoice
			RacunType invoice = Demo.Invoice(Demo.Oib);

			// Send invoice for checking
			var result = Fiscalization.CheckInvoice(invoice, Demo.Certificate, DemoSetup);
		}

		[TestMethod]
		public void TestEchoRequest()
		{
			var msg = "echo message";
			var result = Fiscalization.SendEcho(msg, DemoSetup);

			Assert.AreEqual(msg, result, "Echo method result not equal.");
		}

		[TestMethod]
		public void TestCheckRequestSignature()
		{
			var request = new RacunZahtjev
			{
				Racun = Demo.Invoice(Demo.Oib),
				Zaglavlje = Cis.Fiscalization.GetRequestHeader()
			};

			Fiscalization.Sign(request, Demo.Certificate);

			Assert.IsTrue(Fiscalization.CheckSignature(request), "Checking valid signature failed.");
		}

		[TestMethod]
		public void TestCheckRequestSignatureInvalid()
		{
			var request = new RacunZahtjev
			{
				Racun = Demo.Invoice(Demo.Oib),
				Zaglavlje = Cis.Fiscalization.GetRequestHeader()
			};

			Fiscalization.Sign(request, Demo.Certificate);
			request.Racun.BrRac.BrOznRac = "-42";

			Assert.IsFalse(Fiscalization.CheckSignature(request), "Checking invalid signature failed.");
		}
	}
}
