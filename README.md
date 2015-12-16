# Fiskalizacija API (CIS)

[![cis version][cis-version-image]][porezna-spec]
[![build status][ci-status-image]][ci-url]
[![nuget downloads][nuget-downloads-image]][nuget-url]

.NET (C#) wrapper oko generiranog SOAP klijenta za pozivanje fiskalizacijskog servisa porezne uprave (CIS v1.2).

Sa Microsoft [Wsdl.exe][wsdl.exe] tool-om su generirane proxy klase sa strukturom po WSDL shemi koja je objavljena na stranicama porezne uprave [Tehničke specifikacije][porezna-spec] i koja je uključena u source kôd projekta.  
Preko generiranog SOAP klijenta [FiskalizacijaService][fiscalization-service.cs] se rade svi SOAP pozivi prema __CIS__ servisu. Kompletna implementacija je u [Fiscalization][fiscalization.cs] klasi sa dodatkom async metoda u [Fiscalization.Async.cs][fiscalization-async.cs] file-u. :smile:

Runtime:
- .NET 3.5
- .NET 4.5 (async)

**Testirano i sa demo certifikatom [Fina okoline 2014/2015][fina-demo-2014].**

**Postoji release [.NET COM komponente][docs-com] sa primjerima za __VBA__, __VBScript__ i __Access__**.

## Cilj projekta

* __uključiti source kôd u postojeći projekt__ umjesto referenciranja third party dll-a,
naravno dostupan i kao [NuGet][nuget-url] package
* jednostavan upgrade u slučaju promjene sheme CIS servisa uz __compile-time check__
* svaki poziv servisa treba automatski odraditi __generiranje ZKI kôda i potpisivanje__, isto tako i __provjeru potpisa CIS odgovora__, ali imati i opcije  
`Sign(ICisRequest, X509Certificate2)` i `GenerateZki(RacunType, X509Certificate2)`
* sve __greške__ koje šalje CIS servis pretvoriti u __Exception__-e, unificirati logiranje i imati pregled svih grešaka na jednak način
* ne parsirati raw SOAP poruke, a istovremeno omogućiti potpisivanje i __logiranje raw poruka__

## Instalacija

Kako najjednostavnije uključiti __kôd__ u svoj projekt?!

#### NuGet

Instalacijom [NuGet][nuget-url] package-a kreira se _Cis_ folder u projektu sa source kôdom.

```
PM> Install-Package Cis.Fiscalization
```

[![Nuget screenshot][nuget-screenshot]][nuget-url]

#### Copy

Dovoljno je iskopirati [FiskalizacijaService.cs][fiscalization-service.cs], [Fiscalization.cs][fiscalization.cs] i/ili [Fiscalization.Async.cs][fiscalization-async.cs] (.NET 4.5).

## API

Sastoji od dvije glavne metode (u dva okusa __async__ i sync) za slanje podataka na servis
```cs
public static class Fiscalization
{
    // Async .NET 4.5 :: Fiscalization.Async.cs
    Task<RacunOdgovor> SendInvoiceAsync(RacunType invoice, X509Certificate2 cert);
    Task<PoslovniProstorOdgovor> SendLocationAsync(PoslovniProstorType location, X509Certificate2 cert);

    // Sync :: Fiscalization.cs
    RacunOdgovor SendInvoice(RacunType invoice, X509Certificate2 cert);
    PoslovniProstorOdgovor SendLocation(PoslovniProstorType location, X509Certificate2 cert);
}
```

Svaka metoda još opcionalno prima funkciju `Action<FiskalizacijaService>` za postavljanje
parametara generiranoj proxy klasi __FiskalizacijaService__ (npr. url, timeout...).

Za _RacunType, RacunOdgovor, PoslovniProstorType, PoslovniProstorOdgovor_ i ostale generirane proxy klase vidi source [FiskalizacijaService.cs][fiscalization-service.cs].

Primjer poziva servisa za slanje računa
```cs
// Kreiranje računa za za fiskalizaciju
var invoice = new RacunType()
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

X509Certificate2 certificate = ...;

// Generiraj ZKI, potpiši, pošalji račun i provjeri potpis CIS odgovora
RacunOdgovor response = await Fiscalization.SendInvoiceAsync(invoice, certificate);

// Odgovor sa JIR-om i zahtjevom (sa header podacima, potpisom) i poslanim računom
string jir = response.Jir;
RacunZahtjev request = (RacunZahtjev)response.Request; // ICisRequest
var isTrue = request.Racun == invoice;

// ili dodatno sa postavljanjem opcija
// fs == instanca generirane proxy klase FiskalizacijaService
RacunOdgovor response = await Fiscalization.SendInvoiceAsync(invoice, certificate, fs =>
{
    // SOAP service settings
    // Change service URL
    // default = Fiscalization.SERVICE_URL_PRODUCTION
    fs.Url = Fiscalization.SERVICE_URL_DEMO;

    // Set request timeout in miliseconds
    // default = 100s
    fs.Timeout = 2000;

    // Set response signature checking
    // default = true
    fs.CheckResponseSignature = true;
});
```

## Logiranje raw SOAP poruka

Logiranje se radi preko implementacije partial metode na __FiskalizacijaService__ klasi.
Par linija kôda govori 1000 riječi - __odlazna i dolazna SOAP poruka kao XmlDocument__.

Ovako bi mogao izgledati trace ili file logger
```cs
// MyCisLogger.cs
// Implementacija mora biti u projektu gdje i Cis.FiskalizacijaService klasa
namespace Cis
{
    public partial class FiskalizacijaService
    {
        partial void LogResponseRaw(XmlDocument request, XmlDocument response)
        {
            // Trace logger
            Trace.WriteLine(request.OuterXml);
            Trace.WriteLine(response.OuterXml);

            // File logger
            File.AppendAllText(logFileName, request.OuterXml, Encoding.UTF8);
            File.AppendAllText(logFileName, response.OuterXml, Encoding.UTF8);
        }
    }
}
```

## Testiranje

U [test][test-dir] projektu je [TestEnvironment][test-environment.cs] klasa koja
učitava demo certifikat koji nije uključen u projekt; moguće je
- u source-u specificirati OIB, putanju do certifikata (.pfx), lozinku i/ili
- postaviti `FIS_OIB`, `CERT_BASE64` i `CERT_PWD` _environment_ varijable
```
SET FIS_OIB=<OIB poslovnog subjekta>
SET CERT_BASE64=<certifikat poslovnog subjekta kao base64 enkodirani string>
SET CERT_PWD=<lozinka certifikata>

start Fiscalization.sln
```    

## License

[The MIT License (MIT)][license]

[docs-com]: ./docs/fiscalization-com.md
[docs-com-api]: ./docs/fiscalization-com-api.md
[cis-version-image]: https://cdn.rawgit.com/tgrospic/Cis.Fiscalization/master/docs/cis-service-version.svg
[fiscalization.cs]: ./src/Fiscalization/Cis/Fiscalization.cs
[fiscalization-async.cs]: ./src/Fiscalization/Cis/Fiscalization.Async.cs
[fiscalization-service.cs]: ./src/Fiscalization/Cis/FiskalizacijaService.cs
[test-dir]: ./test/Fiscalization
[test-environment.cs]: ./test/Fiscalization/TestEnvironment.cs
[license]: ./LICENSE

[ci-status-image]: https://ci.appveyor.com/api/projects/status/gumgktf8bs0r4xsm?svg=true
[ci-url]: https://ci.appveyor.com/project/tgrospic/cis-fiscalization
[wsdl.exe]: https://msdn.microsoft.com/en-us/library/7h3ystb6(VS.80).aspx
[porezna-spec]: http://www.porezna-uprava.hr/HR_Fiskalizacija/Stranice/Tehni%C4%8Dke-specifikacije.aspx
[nuget-url]: http://nuget.org/packages/Cis.Fiscalization
[nuget-downloads-image]: https://img.shields.io/nuget/dt/Cis.Fiscalization.svg
[nuget-screenshot]: ./docs/nuget_screenshot.png
[fina-demo-2014]: http://www.fina.hr/Default.aspx?sec=1730
