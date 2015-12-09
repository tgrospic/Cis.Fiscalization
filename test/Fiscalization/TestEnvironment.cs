using Cis;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace FiscalizationTest
{
	// DEMO OIB and certificate is not included in project source code (DemoCertificate.txt)
	// You can paste your OIB and certificate (file name or string)
	// or/and add DemoCertificate.txt with
	// 1. OIB
	// 2. certificate password
	// 3. certificate as base64 encoded string
	public class TestEnvironment
	{
		public string Oib = null; // "92328306173"
		public string CertificateFileName = null; // "DemoCertificate.pfx" or
		public string CertificateString = null; // base64 encoded certificate
		public string CertificatePassword = null;

		public X509Certificate2 Certificate = null;

		private TestEnvironment() { }

		public static TestEnvironment Create()
		{
			var env = new TestEnvironment();
			env.LoadCertificateData();

			return env;
		}

		public void LoadCertificateData()
		{
			var demoInfoFileName = "DemoCertificate.txt";

			if (File.Exists(demoInfoFileName))
			{
				var lines = File.ReadAllLines(demoInfoFileName);

				// Fun with λ :)
				AssignEach(lines,
					() => this.Oib,
					() => this.CertificatePassword,
					() => this.CertificateString
				);
			}

			if (this.CertificateFileName != null)
			{
				// Get certificate from file
				this.Certificate = new X509Certificate2(this.CertificateFileName, this.CertificatePassword);
			}
			else if (this.CertificateString != null)
			{
				// Get certificate from string
				var raw = Convert.FromBase64String(this.CertificateString);
				this.Certificate = new X509Certificate2(raw, this.CertificatePassword);
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
				IznosUkupno = 3.ToString("N2", CultureInfo.InvariantCulture),
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
						Iznos = 0.59.ToString("N2", CultureInfo.InvariantCulture),
					}
				},
				USustPdv = true
			};
		}

		public PoslovniProstorType Location(string oib)
		{
			return new PoslovniProstorType()
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
				DatumPocetkaPrimjene = DateTime.Now.AddDays(-60).ToString(Fiscalization.DATE_FORMAT_SHORT),
				Oib = oib,
				OznPoslProstora = "1",
				RadnoVrijeme = "radno vrijeme",
				SpecNamj = "112343454"
			};
		}

		#endregion

		#region Helpers

		void AssignEach(string[] lines, params Expression<Func<string>>[] fieldSelectors)
		{
			ZipIter(lines, fieldSelectors, (line, selector) =>
			{
				var exp = (MemberExpression)selector.Body;
				var fieldTy = (FieldInfo)exp.Member;
				var propVal = (string)fieldTy.GetValue(this);

				if (string.IsNullOrEmpty(propVal))
					fieldTy.SetValue(this, line);
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
