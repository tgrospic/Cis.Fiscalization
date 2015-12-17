'.NET COM Interop
Dim cisInterop
Set cisInterop = CreateObject("FiscalizationComInterop")

' Set logging
cisInterop.LogFileName = "Fiscal.log"

' Demo OIB & certificate
Set objFSO = CreateObject("Scripting.FileSystemObject")
Set env = WScript.CreateObject("WScript.Shell").Environment("PROCESS")

Dim oib
oib = env("FIS_OIB")

Dim certPwd
certPwd = env("CERT_PWD")

Dim certBase64
certBase64 = env("CERT_BASE64")

' Get certificate from file or base64 encoded string
Dim cert
'Set cert = cisInterop.GetCertificateFile("<certificate file name>.pfx", "<password>")
Set cert = cisInterop.GetCertificateString(certBase64, certPwd)

' Create address
Dim address' As AdresaType
Set address = CreateObject("Cis.AdresaType")
With address
	.Ulica = "Ulica"
	.BrojPoste = "10000"
	.Naselje = "Naselje"
	.Opcina = "Opcina"
End With

' Create AdresniPodatakType
Dim addressType
Set addressType = CreateObject("Cis.AdresniPodatakType")
With addressType
	.Item = address
End With

' Create LocationType
Dim loc
Set loc = CreateObject("Cis.PoslovniProstorType")
With loc
	.AdresniPodatak = addressType
	.OznPoslProstora = "1"
	.Oib = oib
	.RadnoVrijeme = "radno vrijeme"
	.DatumPocetkaPrimjene = cisInterop.DateFormatShort(Date)
	.SpecNamj = "112343454"
	.OznakaZatvaranja = cisInterop.OznakaZatvaranjaType_Z
	.OznakaZatvaranjaSpecified = True
End With

' Send request
Dim result 'As PoslovniProstorOdgovor
Set result = cisInterop.SendLocation((loc), (cert), 0, True)

MsgBox ("Id poruke: " + result.Zaglavlje.IdPoruke)
