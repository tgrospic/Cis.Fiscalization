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
		public async Task TestInvoiceRequestAsync()
		{
			// Create demo invoice
			RacunType invoice = Demo.Invoice(Demo.Oib);

			var result = await Fiscalization.SendInvoiceAsync(invoice, Demo.Certificate, DemoSetup);

			Assert.IsNotNull(result, "Result is null.");
			Assert.IsNotNull(result.Jir, "JIR is null.");
		}

		[TestMethod]
		[ExpectedException(typeof(ApplicationException), "Invalid OIB sent to CIS service.")]
		public async Task TestInvalidInvoiceRequestAsync()
		{
			// Create demo invoice with invalid OIB, must throw exception (message from GreskaType)
			RacunType invoice = Demo.Invoice("invalid OIB");

			await Fiscalization.SendInvoiceAsync(invoice, Demo.Certificate, DemoSetup);
		}

		[TestMethod]
		public async Task TestLocationRequestAsync()
		{
			PoslovniProstorType location = Demo.Location(Demo.Oib);

			var result = await Fiscalization.SendLocationAsync(location, Demo.Certificate, DemoSetup);

			Assert.IsNotNull(result, "Result is null.");
		}

		[TestMethod]
		[ExpectedException(typeof(ApplicationException), "Invalid OIB sent to CIS service.")]
		public async Task TestInvalidLocationRequestAsync()
		{
			PoslovniProstorType location = Demo.Location("invalid OIB");

			await Fiscalization.SendLocationAsync(location, Demo.Certificate, DemoSetup);
		}

		[TestMethod]
		public async Task TestEchoRequestAsync()
		{
			var msg = "echo message";
			var result = await Fiscalization.SendEchoAsync(msg, DemoSetup);

			Assert.AreEqual(msg, result, "Echo method result not equal.");
		}

		// Test sync API
		[TestMethod]
		public void TestInvoiceRequest()
		{
			// Create demo invoice
			RacunType invoice = Demo.Invoice(Demo.Oib);

			// Send request with response signature checking
			var result = Fiscalization.SendInvoice(invoice, Demo.Certificate, DemoSetup);

			Assert.IsNotNull(result, "Result is null.");
			Assert.IsNotNull(result.Jir, "JIR is null.");
		}

		[TestMethod]
		[ExpectedException(typeof(ApplicationException), "Invalid OIB sent to CIS service.")]
		public void TestInvalidInvoiceRequest()
		{
			// Create demo invoice with invalid OIB, must throw exception (message from GreskaType)
			RacunType invoice = Demo.Invoice("invalid OIB");

			Fiscalization.SendInvoice(invoice, Demo.Certificate, DemoSetup);
		}

		[TestMethod]
		public void TestLocationRequest()
		{
			PoslovniProstorType location = Demo.Location(Demo.Oib);

			PoslovniProstorOdgovor result = Fiscalization.SendLocation(location, Demo.Certificate, DemoSetup);

			Assert.IsNotNull(result, "Result is null.");
		}

		[TestMethod]
		[ExpectedException(typeof(ApplicationException), "Invalid OIB sent to CIS service.")]
		public void TestInvalidLocationRequest()
		{
			PoslovniProstorType location = Demo.Location("invalid OIB");

			Fiscalization.SendLocation(location, Demo.Certificate, DemoSetup);
		}

		[TestMethod]
		public void TestEchoRequest()
		{
			var msg = "echo message";
			var result = Fiscalization.SendEcho(msg, DemoSetup);

			Assert.AreEqual(msg, result, "Echo method result not equal.");
		}
	}
}
