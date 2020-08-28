using Cis;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace FiscalizationTest
{
	// DEMO OIB and certificate is not included in project source code
	// You can paste your OIB and certificate (file name or base64 string)
	// or/and set environment variables
	// > SET FIS_OIB=<oib>
	// > SET CERT_BASE64=<base64 encoded certificate>
	// > SET CERT_PWD=<certificate password>
	// > start Fiscalization.sln
	public class TestEnvironment
	{
		public string Oib = null; // "92328306173"
		public string CertificateFileName = null; // "DemoCertificate.pfx" or
		public string CertificateBase64 = null; // base64 encoded certificate
		public string CertificatePassword = null;

		public X509Certificate2 Certificate = null;

		private TestEnvironment() { }

		static TestEnvironment()
		{
			// MUST be set for .NetFramework 4.7 and latest! Otherwise we get "System.Security.Cryptography.CryptographicException: Invalid algorithm specified."
			AppContext.SetSwitch( "Switch.System.Security.Cryptography.Xml.UseInsecureHashAlgorithms", true );
			AppContext.SetSwitch( "Switch.System.Security.Cryptography.Pkcs.UseInsecureHashAlgorithms", true );

			// Unconditionally trust the server certificate
			ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback((sender, cert, chain, sslPolicyErrors) => true);
		}

		public static TestEnvironment Create()
		{
			var env = new TestEnvironment();
			env.LoadCertificateData();

			return env;
		}

		public void LoadCertificateData()
		{
			// Fun with λ :)
			AssignEnvironment(new[]
				{
				"FIS_OIB",
				"CERT_BASE64",
				"CERT_PWD"
				},
				() => Oib,
				() => CertificateBase64,
				() => CertificatePassword
			);

			if (CertificateFileName != null)
			{
				// Get certificate from file
				Certificate = new X509Certificate2(CertificateFileName, CertificatePassword);
			}
			else if (CertificateBase64 != null)
			{
				// Get certificate from string
				var raw = Convert.FromBase64String(CertificateBase64);
				Certificate = new X509Certificate2(raw, CertificatePassword);
			}
		}

		#region Test data

		public RacunType Invoice(string oib)
		{
			return new RacunType()
			{
				BrRac = new BrojRacunaType()
				{
					BrOznRac = "1",
					OznPosPr = "1",
					OznNapUr = "1"
				},
				DatVrijeme = DateTime.Now.ToString(Fiscalization.DATE_FORMAT_LONG),
				IznosUkupno = 2.9.ToString("N2", CultureInfo.InvariantCulture),
				NacinPlac = NacinPlacanjaType.G,
				NakDost = false,
				Oib = oib,
				OibOper = "98642375382",
				OznSlijed = OznakaSlijednostiType.N,
				Pdv = new[]
				{
					new PorezType
					{
						Stopa = 25.ToString("N2", CultureInfo.InvariantCulture),
						Osnovica = 2.34.ToString("N2", CultureInfo.InvariantCulture),
						Iznos = .56.ToString("N2", CultureInfo.InvariantCulture),
					}
				},
				USustPdv = true
			};
		}

		#endregion

		#region Helpers

		void AssignEnvironment(string[] envNames, params Expression<Func<string>>[] fieldSelectors)
		{
			ZipIter(envNames, fieldSelectors, (name, selector) =>
			{
				var exp = (MemberExpression)selector.Body;
				var fieldTy = (FieldInfo)exp.Member;
				var propVal = (string)fieldTy.GetValue(this);

				if (string.IsNullOrEmpty(propVal))
					fieldTy.SetValue(this, Environment.GetEnvironmentVariable(name));
			});
		}

		// Quick replacement for Zip (.NET 4>)
		void ZipIter<T, S>(IEnumerable<T> fst, IEnumerable<S> snd, Action<T, S> selector)
		{
			var sndEnumerator = snd.GetEnumerator();
			foreach (var fstVal in fst.Where(_ => sndEnumerator.MoveNext()))
				selector(fstVal, sndEnumerator.Current);
		}

		#endregion
	}
}
