using PortableDeveloper.Application.ProjectTools;

namespace PortableDeveloper.App.ViewModels;

public sealed partial class UiText
{
    public string OpenProjectDirectory => IsCzech ? "Otevřít projekt" : "Open project";

    public string EditCustomPhpIni => IsCzech ? "Upravit vlastní php.ini" : "Edit custom php.ini";

    public string CustomPhpIniHelp => IsCzech
        ? "Soubor se připojí za bezpečně generovaný php.ini při každém startu Apache. Ruční direktivy mohou přepsat hodnoty z formuláře a použijí se po příštím spuštění nebo restartu Apache."
        : "This file is appended after the safely generated php.ini whenever Apache starts. Manual directives can override form values and take effect after Apache starts or restarts.";

    public string InstalledPackages => IsCzech ? "Nainstalované knihovny" : "Installed packages";

    public string NoInstalledPackages => IsCzech
        ? "V tomto projektu zatím nejsou nainstalované žádné knihovny."
        : "No libraries are installed in this project yet.";

    public string AddPackage => IsCzech ? "Přidat knihovnu" : "Add package";

    public string PackageName => IsCzech ? "Název balíčku" : "Package name";

    public string VersionConstraint => IsCzech ? "Verze / omezení (volitelné)" : "Version / constraint (optional)";

    public string InstallPackage => IsCzech ? "Nainstalovat" : "Install";

    public string RemovePackage => IsCzech ? "Odebrat" : "Remove";

    public string TransitiveDependencies => IsCzech ? "Použité závislosti" : "Used dependencies";

    public string DirectDependency => IsCzech ? "Přímá závislost" : "Direct dependency";

    public string ComposerHelp => IsCzech
        ? "Balíčky se instalují do vendor aktuálního projektu. Každý projekt má vlastní composer.json a závislosti."
        : "Packages are installed into the active project's vendor directory. Every project has its own composer.json and dependencies.";

    public string ComposerPackageExample => "php-webdriver/webdriver";

    public string ComposerConstraintExample => IsCzech ? "např. ^1.15" : "e.g. ^1.15";

    public string PythonHelp => IsCzech
        ? "Projektové balíčky se instalují do instances/default/python/packages. Základní Python ani systémový profil Windows se nemění."
        : "Project packages are installed into instances/default/python/packages. The base Python runtime and Windows user profile are not modified.";

    public string NodeHelp => IsCzech
        ? "Balíčky npm se instalují do node_modules aktuálního projektu. Každý projekt má vlastní package.json a package-lock.json; instalační skripty balíčků jsou z bezpečnostních důvodů vypnuté."
        : "npm packages are installed into the active project's node_modules. Every project has its own package.json and package-lock.json; package install scripts are disabled for safety.";

    public string NodePackageExample => IsCzech ? "např. lodash" : "e.g. lodash";

    public string NodeConstraintExample => IsCzech ? "např. ^4.17.21" : "e.g. ^4.17.21";

    public string PythonPackageExample => IsCzech ? "např. selenium" : "e.g. selenium";

    public string PythonConstraintExample => IsCzech ? "např. ==4.35.0" : "e.g. ==4.35.0";

    public string PackageNetworkNotice => IsCzech
        ? "Instalace a odebrání jsou explicitní uživatelské akce. Mohou používat internet a spouštět instalační logiku balíčku; vybírejte jen důvěryhodné knihovny."
        : "Install and remove are explicit user actions. They may use the internet and execute package installation logic; choose trusted libraries only.";

    public string RefreshPackages => IsCzech ? "Obnovit přehled" : "Refresh packages";

    public string LoadingPackages => IsCzech ? "Načítám nainstalované knihovny…" : "Loading installed packages…";

    public string PackageOperationProgress(ProjectPackageOperationProgress progress)
    {
        var packageName = string.IsNullOrWhiteSpace(progress.PackageName) ? null : progress.PackageName;
        return
        (progress.Operation, progress.Phase) switch
        {
            (_, ProjectPackageOperationPhase.Preparing) =>
                IsCzech
                    ? packageName is null ? "Připravuji operaci s knihovnou…" : $"Připravuji {packageName}…"
                    : packageName is null ? "Preparing package operation…" : $"Preparing {packageName}…",
            (ProjectPackageOperationKind.Install, ProjectPackageOperationPhase.RunningPackageManager) =>
                IsCzech
                    ? packageName is null ? "Řeším závislosti a instaluji knihovnu…" : $"Instaluji {packageName} a jeho závislosti…"
                    : packageName is null ? "Resolving dependencies and installing package…" : $"Installing {packageName} and its dependencies…",
            (ProjectPackageOperationKind.Remove, ProjectPackageOperationPhase.RunningPackageManager) =>
                IsCzech
                    ? packageName is null ? "Odebírám knihovnu a upravuji závislosti…" : $"Odebírám {packageName} a upravuji závislosti…"
                    : packageName is null ? "Removing package and updating dependencies…" : $"Removing {packageName} and updating dependencies…",
            (_, ProjectPackageOperationPhase.RefreshingInventory) => LoadingPackages,
            (ProjectPackageOperationKind.Refresh, ProjectPackageOperationPhase.Completed) =>
                IsCzech ? "Přehled knihoven je aktuální." : "Package inventory is up to date.",
            (_, ProjectPackageOperationPhase.Completed) =>
                IsCzech ? "Operace správce balíčků byla dokončena." : "Package manager operation completed.",
            _ => IsCzech ? "Probíhá operace s knihovnou…" : "Package operation in progress…"
        };
    }

    public string PackageOperationDetail(ProjectPackageOperationProgress progress, string fallbackPackageName = "")
    {
        var packageName = string.IsNullOrWhiteSpace(progress.PackageName)
            ? fallbackPackageName
            : progress.PackageName;

        return string.IsNullOrWhiteSpace(packageName)
            ? string.Empty
            : IsCzech ? $"Knihovna: {packageName}" : $"Package: {packageName}";
    }

    public string PackageListFailed(string detail) => IsCzech
        ? $"Přehled knihoven se nepodařilo načíst: {detail}"
        : $"The package list could not be loaded: {detail}";

    public string PackageOperationFailed(string detail) => IsCzech
        ? $"Operace s knihovnou selhala: {detail}"
        : $"The package operation failed: {detail}";

    public string PackageInstalled(string name) => IsCzech
        ? $"Knihovna {name} byla nainstalována."
        : $"Package {name} was installed.";

    public string PackageOperationSucceeded(string name, PackageOperationOutcome outcome) => outcome switch
    {
        PackageOperationOutcome.PromotedToDirect => IsCzech
            ? $"Knihovna {name} už byla přítomná a nyní je přímým požadavkem projektu."
            : $"Package {name} was already present and is now a direct project requirement.",
        PackageOperationOutcome.AlreadyDirect => IsCzech
            ? $"Knihovna {name} už je přímým požadavkem projektu."
            : $"Package {name} is already a direct project requirement.",
        _ => PackageInstalled(name)
    };

    public string PackageRemoved(string name) => IsCzech
        ? $"Knihovna {name} byla odebrána."
        : $"Package {name} was removed.";

    public string RemovePackageQuestion(string name) => IsCzech
        ? $"Opravdu odebrat knihovnu {name} z tohoto projektu?"
        : $"Remove package {name} from this project?";

    public string RemovePackageTitle => IsCzech ? "Odebrání knihovny" : "Remove package";

}
