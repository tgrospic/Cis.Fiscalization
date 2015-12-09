# Projekt FiscalizationCom

.NET COM komponenta za fiskalizaciju sa primjerima za __VBA__, __VBScript__ i __Access__ (verzija u primjeru 2010).

## Cilj projekta

* koristiti API za fiskalizaciju direktno iz jezika u kojem je aplikacija napisana preko COM komponente
* izbjegnuti manipulaciju sa XML-om, sve operacije se rade preko objekata u jeziku aplikacije
* sve greške su exception-i u jeziku aplikacije, nema dodatnog kôda za parsiranje grešaka
* mogućnost logiranja raw SOAP poruka sa CIS servisa

## Instalacija

Download [FiscalizationCom][release-latest] ([zip][download-com]) u kojem je

- __FiscalizationCom.dll__
- batch skripte za registraciju - __RegisterCOM.cmd__, __UnRegisterCOM.cmd__
- primjeri za VBA i VBScript, Access (Test.accdb)

##### Registracija i referenciranje COM komponente

```bat
REM x86
%SYSTEMROOT%\Microsoft.NET\Framework\v2.0.50727\RegAsm.exe "%~dp0FiscalizationCom.dll" /codebase /tlb
REM x64
%SYSTEMROOT%\Microsoft.NET\Framework64\v2.0.50727\RegAsm.exe "%~dp0FiscalizationCom.dll" /codebase /tlb
```
- __FiscalizationCom.tlb__ se kreira nakon registracije sa _RegisterCOM.cmd_ skriptom
- u __VBA__ editoru se referencira _FiscalizationCom.tlb_ file
- za pozivanje iz __VBScript__-a _FiscalizationCom.tlb_ file nije potreban (nije potrebno ni registrirati sa opcijom /tlb)

## API

Ovdje je kompletan [COM API][com-api].
Metode za poziv servisa i pomoćne metode su prilagođene za pozive preko COM-a sa __FiscalizationComInterop__ klasom.
Svejedno je jel se koristi uvijek ista instanca ili se za svaki poziv kreira nova.

```vb
' .NET COM Interop
Dim cisInterop As New FiscalizationComInterop

' Slanje računa
Set result = cisInterop.SendInvoice(RacunType, (X509Certificate2), timeout/ms: Int, isDemo: Bool, check_response_signature: Bool)

' Slanje poslovnog prostora
Set result = cisInterop.SendLocation(PoslovniProstorType, (X509Certificate2), timeout/ms: Int, isDemo: Bool, check_response_signature: Bool)

' Echo
Set result = cisInteropSendEcho(String, timeout/ms: Int, isDemo: Bool)
```

### Logiranje

Logiranje odlaznih i dolaznih SOAP poruka u file.
Postavlja se na razini instance __FiscalizationComInterop__ klase.

```vb
' .NET COM Interop
Dim cisInterop As New FiscalizationComInterop

' Set logging
cisInterop.LogFileName = "Fiscal.log"
```

### Primjer VBScript

```vb
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

' Create taxes
Dim pdv25
Set pdv25 = CreateObject("Cis.PorezType")
With pdv25
  .Stopa = "25.00"
  .Osnovica = "10.00"
  .Iznos = "2.50"
End With
Dim taxes(1)
Set taxes(0) = pdv25

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
  ' Convert Variant() to PorezType[]
  .Pdv = cisInterop.ToPorezTypeArray((taxes))
End With

' Send invoice
Dim result 'As RacunOdgovor
Set result = cisInterop.SendInvoice((invoice), (cert), 0, True)

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
```

[release-latest]: https://github.com/tgrospic/Cis.Fiscalization/releases/tag/v1.2.0
[download-com]:   https://github.com/tgrospic/Cis.Fiscalization/releases/download/v1.2.0/FiscalizationCom-v1.2.0.zip
[com-api]: ./fiscalization-com-api.md
