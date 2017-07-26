# COM API specifikacija

Ovdje je cijeli COM API zajedno sa helper metodama i field-ovima. Za strukturu generiranih proxy objekata (_RacunType, RacunZahtjev, RacunOdgovor, ProvjeraOdgovor, ...) vidi source [FiskalizacijaService.cs][fiscalization-service.cs] ili WSDL shemu.

## FiscalizationComInterop
```vb
' Slanje računa
RacunOdgovor SendInvoice(RacunType, (X509Certificate2), timeout/ms: Int, isDemo: Bool, check_response_signature: Bool)
RacunOdgovor SendInvoiceRequest(RacunZahtjev, (X509Certificate2), timeout/ms: Int, isDemo: Bool, check_response_signature: Bool)

' Provjera računa
ProvjeraOdgovor CheckInvoice(RacunType, (X509Certificate2), timeout/ms: Int, isDemo: Bool, check_response_signature: Bool)
ProvjeraOdgovor CheckInvoiceRequest(RacunZahtjev, (X509Certificate2), timeout/ms: Int, isDemo: Bool, check_response_signature: Bool)

' Echo
String SendEcho(String, timeout/ms: Int, isDemo: Bool)
```

#### Pomoćne metode
```vb
' Generiranje ZKI kôda na računu
GenerateZki(RacunType, (X509Certificate2))

' Kreiranje zahtjeva za račun (ako se koristi SendInvoiceRequest)
RacunZahtjev CreateInvoiceRequest(RacunType)

' Kreiranje zahtjeva za provjeru račun (ako se koristi CheckInvoiceRequest)
ProvjeraZahtjev CreateCheckRequest(RacunType)

' Potpisivanje zatjeva (RacunZahtjev ili PoslovniProstorZahtjev)
Sign(zahtjev: ICisRequest, (X509Certificate2))

' Formatiranje datuma u string
' CIS servis sve datume prima kao string - čija je to pametna ideja?!
' isto vrijedi i za brojčane vrijednosti??!!
String DateFormatLong(DateTime)
String DateFormatShort(DateTime)

' Dohvat certifikata i export certifikata
X509Certificate2 GetCertificateRaw(rawCert: byte[], password: String)
X509Certificate2 GetCertificateString(base64Encoded: String, password: String)
X509Certificate2 GetCertificateFile(fileName: String, password: String)
String ExportCertificate(fileName: String, password: String, exportPassword: String)
```

#### Enumeracije su mapirane kao field-ovi
```vb
' Oznaka slijednosti
OznakaSlijednostiType_N
OznakaSlijednostiType_P

' Način plaćanja
NacinPlacanjaType_C
NacinPlacanjaType_G
NacinPlacanjaType_K
NacinPlacanjaType_O
NacinPlacanjaType_T
```

#### VBScript - pomoćne metode za konverziju Variant() array-a
Potrebno za popunjavanje poreza na računu
```vb
PorezType[] ToPorezTypeArray(object[] variantArray)
PorezOstaloType[] ToPorezOstaloTypeArray(object[] variantArray)
NaknadaType[] ToNaknadaTypeArray(object[] variantArray)
```

[fiscalization-service.cs]: ../src/Fiscalization/Cis/FiskalizacijaService.cs

