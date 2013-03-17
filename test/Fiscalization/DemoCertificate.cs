// Fiscalization API CIS 2012
// http://fiscalization.codeplex.com/
// Copyright (c) 2013 Tomislav Grospic

using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace FiscalizationTest
{
	// DEMO OIB and certificate not included in project source code(DemoCertificate.txt)
	// You can paste your OIB and certificate(file or string)
	// or/and add DemoCertificate.txt
	// (first line OIB, second line certificate password, third line certificate as base64 encoded string)
	public class DemoCertificate
	{
		public string Oib = null; // "92328306173"

		public string CertificateFileName = null; // "DemoCertificate.pfx"
		public string CertificatePassword = null;
		public string CertificateString = null;

		public X509Certificate2 Certificate = null;

		public static DemoCertificate GetInfo()
		{
			var demoInfoFileName = "DemoCertificate.txt";
			var demoInfo = new DemoCertificate();
			
			if (File.Exists(demoInfoFileName))
			{
				var result = File.ReadAllLines(demoInfoFileName);
				if (demoInfo.Oib == null)
					demoInfo.Oib = result[0];

				if (demoInfo.CertificatePassword == null)
					demoInfo.CertificatePassword = result[1];

				if (demoInfo.CertificateString == null)
					demoInfo.CertificateString = result[2];
			}

			if (demoInfo.CertificateString != null)
			{
				// Get certificate from string
				var raw = Convert.FromBase64String(demoInfo.CertificateString);
				demoInfo.Certificate = new X509Certificate2(raw, demoInfo.CertificatePassword);
			}

			if (demoInfo.CertificateFileName != null)
			{
				// Get certificate from file
				demoInfo.Certificate = new X509Certificate2(demoInfo.CertificateFileName, demoInfo.CertificatePassword);
			}

			return demoInfo;
		}
	}
}
