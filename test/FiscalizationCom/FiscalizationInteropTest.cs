using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FiscalizationTest
{
	[TestClass]
	public class FiscalizationInteropTest
	{
		TestEnvironment Demo = TestEnvironment.Create();

		[TestMethod]
		public void TestInvoiceRequest()
		{
			var com = new FiscalizationComInterop();

			var result = com.SendInvoice(Demo.Invoice(Demo.Oib), Demo.Certificate, timeout: 0, isDemo: true, checkResponseSignature: true);

			Assert.IsNotNull(result, "Result is null.");
			Assert.IsNotNull(result.Jir, "JIR is null.");
		}

		[TestMethod]
		public void TestCheckInvoiceRequest()
		{
			var com = new FiscalizationComInterop();
			var invoice = Demo.Invoice(Demo.Oib);

			var _ = com.SendInvoice(invoice, Demo.Certificate, timeout: 0, isDemo: true, checkResponseSignature: true);

			var result = com.CheckInvoice(invoice, Demo.Certificate, timeout: 0, isDemo: true, checkResponseSignature: true);

			Assert.IsNotNull(result, "Result is null.");
		}

		[TestMethod]
		public void TestEchoRequest()
		{
			var com = new FiscalizationComInterop();
			var msg = "echo message";
			var result = com.SendEcho(msg, timeout: 0, isDemo: true);

			Assert.AreEqual(msg, result, "Echo method result not equal.");
		}
	}
}
