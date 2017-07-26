// Cis.Fiscalization v1.3.0 :: CIS WSDL v1.4 (2012-2017)
// https://github.com/tgrospic/Cis.Fiscalization
// Copyright (c) 2013-present Tomislav Grospic
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Cis
{
	public static partial class Fiscalization
	{
		#region Service Async API

		/// <summary>
		/// Send invoice request async
		/// </summary>
		/// <param name="request">Request to send</param>
		/// <param name="certificate">Signing certificate, optional if request is already signed</param>
		/// <param name="setupService">Function to set service settings</param>
		public static Task<RacunOdgovor> SendInvoiceRequestAsync(RacunZahtjev request, X509Certificate2 certificate = null,
			Action<FiskalizacijaService> setupService = null)
		{
			if (request == null) throw new ArgumentNullException("request");
			if (request.Racun == null) throw new ArgumentNullException("request.Racun");

			return SignAndSendRequestAsync<RacunZahtjev, RacunOdgovor>(request, x => x.RacuniAsync, certificate, setupService);
		}

		/// <summary>
		/// Send invoice async
		/// </summary>
		/// <param name="invoice">Invoice to send</param>
		/// <param name="certificate">Signing certificate</param>
		/// <param name="setupService">Function to set service settings</param>
		public static Task<RacunOdgovor> SendInvoiceAsync(RacunType invoice, X509Certificate2 certificate,
			Action<FiskalizacijaService> setupService = null)
		{
			if (invoice == null) throw new ArgumentNullException("invoice");
			if (certificate == null) throw new ArgumentNullException("certificate");

			var request = new RacunZahtjev
			{
				Racun = invoice,
				Zaglavlje = Cis.Fiscalization.GetRequestHeader()
			};

			return SendInvoiceRequestAsync(request, certificate, setupService);
		}

		/// <summary>
		/// Check invoice request async
		/// </summary>
		/// <param name="request">Request to send</param>
		/// <param name="certificate">Signing certificate, optional if request is already signed</param>
		/// <param name="setupService">Function to set service settings</param>
		public static Task<ProvjeraOdgovor> CheckInvoiceRequestAsync(ProvjeraZahtjev request, X509Certificate2 certificate = null,
			Action<FiskalizacijaService> setupService = null)
		{
			if (request == null) throw new ArgumentNullException("request");
			if (request.Racun == null) throw new ArgumentNullException("request.Racun");

			return SignAndSendRequestAsync<ProvjeraZahtjev, ProvjeraOdgovor>(request, x => x.ProvjeraAsync, certificate, setupService);
		}

		/// <summary>
		/// Send invoice async
		/// </summary>
		/// <param name="invoice">Invoice to check</param>
		/// <param name="certificate">Signing certificate</param>
		/// <param name="setupService">Function to set service settings</param>
		public static Task<ProvjeraOdgovor> CheckInvoiceAsync(RacunType invoice, X509Certificate2 certificate,
			Action<FiskalizacijaService> setupService = null)
		{
			if (invoice == null) throw new ArgumentNullException("invoice");
			if (certificate == null) throw new ArgumentNullException("certificate");

			var request = new ProvjeraZahtjev
			{
				Racun = invoice,
				Zaglavlje = Cis.Fiscalization.GetRequestHeader()
			};

			return CheckInvoiceRequestAsync(request, certificate, setupService);
		}

		/// <summary>
		/// Send echo request async
		/// </summary>
		/// <param name="echo">String to send</param>
		/// <param name="setupService">Function to set service settings</param>
		public static Task<string> SendEchoAsync(string echo, Action<FiskalizacijaService> setupService = null)
		{
			if (echo == null) throw new ArgumentNullException("echo");

			// Create service endpoint
			var fs = new FiskalizacijaService();
			if (setupService != null)
				setupService(fs);

			// Response is not signed
			fs.CheckResponseSignature = false;

			// Send request
			return fs.EchoAsync(echo);
		}

		#endregion

		#region Send methods (generic)

		/// <summary>
		/// Send request async
		/// </summary>
		/// <typeparam name="TRequest">Type of service method argument</typeparam>
		/// <typeparam name="TResponse">Type of service method result</typeparam>
		/// <param name="request">Request to send</param>
		/// <param name="serviceMethod">Function to provide service method</param>
		/// <param name="certificate">Signing certificate</param>
		/// <param name="setupService">Function to set service settings</param>
		/// <returns>Service response object</returns>
		public static async Task<TResponse> SignAndSendRequestAsync<TRequest, TResponse>(TRequest request,
			Func<FiskalizacijaService, Func<TRequest, Task<TResponse>>> serviceMethod, X509Certificate2 certificate = null,
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
			var result = await method(request);

			// Add reference to request object
			result.Request = request;

			ThrowOnResponseErrors(result);

			return result;
		}

		/// <summary>
		/// Send request (sync) using async service method
		/// TODO: Test
		/// </summary>
		/// <typeparam name="TRequest">Type of service method argument</typeparam>
		/// <typeparam name="TResponse">Type of service method result</typeparam>
		/// <param name="request">Request to send</param>
		/// <param name="serviceMethod">Function to provide service method</param>
		/// <param name="certificate">Signing certificate</param>
		/// <param name="setupService">Function to set service settings</param>
		/// <returns>Service response object</returns>
		public static TResponse SignAndSendRequest<TRequest, TResponse>(TRequest request,
			Func<FiskalizacijaService, Func<TRequest, Task<TResponse>>> serviceMethod, X509Certificate2 certificate = null,
			Action<FiskalizacijaService> setupService = null)
			where TRequest : ICisRequest
			where TResponse : ICisResponse
		{
			var task = SignAndSendRequestAsync(request, serviceMethod, certificate, setupService);

			try
			{
				// Wait for task to end
				task.Wait();
			}
			catch (AggregateException aggEx)
			{
				// We are sure that only one error exist
				throw aggEx.InnerException;
			}

			return task.Result;
		}

		#endregion
	}

	#region FiskalizacijaService partial implementation

	public partial class FiskalizacijaService
	{
		#region Async (TPL) version of main methods

		public Task<RacunOdgovor> RacuniAsync(RacunZahtjev request)
		{
			return Task.Factory.FromAsync(Beginracuni, Endracuni, request, null);
		}

		public Task<ProvjeraOdgovor> ProvjeraAsync(ProvjeraZahtjev request)
		{
			return Task.Factory.FromAsync(Beginprovjera, Endprovjera, request, null);
		}

		public Task<string> EchoAsync(string request)
		{
			return Task.Factory.FromAsync(Beginecho, Endecho, request, null);
		}

		#endregion
	}

	#endregion
}
