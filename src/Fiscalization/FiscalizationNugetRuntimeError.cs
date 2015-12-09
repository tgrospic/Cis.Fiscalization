// This marker file is installed if the project is build with
// insufficient .NET runtime version <.NET3.5
class FiscalizationNugetRuntimeError
{
	"Ovo je namjerno generirana greška (Cis.Fiscalization)."

	public FiscalizationNugetRuntimeError()
	{
		var error = new
		{
			version = "Za Cis.Fiscalization potrebna je minimalna verzija .NET-a 3.5 (LINQ) ili 4.5 (Task).",

			nugetReinstall = "Reinstalacija nuget package-a u Package Manager Console",

			PackageManagerConsole = "Update-Package –reinstall Cis.Fiscalization"
		};
	}
}
