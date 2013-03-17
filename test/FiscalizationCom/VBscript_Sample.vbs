' Fiscalization API CIS 2012
' http://fiscalization.codeplex.com/
' Copyright (c) 2013 Tomislav Grospic

'.NET COM Interop
Dim cisInterop
Set cisInterop = CreateObject("FiscalizationComInterop")

' Set logging
cisInterop.LogFileName = "Fiscal.log"

' Demo OIB & certificate
Set objFSO = CreateObject("Scripting.FileSystemObject")
Set objFile = objFSO.OpenTextFile("DemoCertificate.txt", 1)

Dim oib
oib = objFile.ReadLine

Dim certPwd
certPwd = objFile.ReadLine

Dim certBase64
certBase64 = objFile.ReadLine

' Get certificate from file or base64 encoded string
Dim cert
'Set cert = cisInterop.GetCertificateFile("<certificate file name>.pfx", "<password>")
Set cert = cisInterop.GetCertificateString(certBase64, certPwd)

' Create invoice number
Dim invoiceNr' As BrojRacunaType
Set invoiceNr = CreateObject("Cis.BrojRacunaType")
With invoiceNr
	.BrOznRac = "1"
	.OznPosPr = "1"
	.OznNapUr = "1"
End With
	
' Create Racun
Dim invoice
Set invoice = CreateObject("Cis.RacunType")
With invoice
	.OIB = oib
	.USustPdv = True
	.IznosUkupno = "123.45"
	.DatVrijeme = cisInterop.DateFormatLong(Date)
	.OznSlijed = cisInterop.OznakaSlijednostiType_N
	.NacinPlac = cisInterop.NacinPlacanjaType_G
	.OibOper = "98642375382"
	.NakDost = False
	.BrRac = invoiceNr
End With

' invoice.ZastKod <- filled with generated ZKI code - optional
Call cisInterop.GenerateZki((invoice), (cert))

Dim request' As RacunZahtjev
Set request = cisInterop.CreateInvoiceRequest((invoice))
	
' Call GenerateZki and Sign request - optional
Call cisInterop.Sign((request), (cert))
	
' Send request
Dim result 'As RacunOdgovor
Set result = cisInterop.SendInvoiceRequest((request), (cert), 0, True)
	
MsgBox (result.Jir)