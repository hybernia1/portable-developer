using PortableDeveloper.Application.Packages;

namespace PortableDeveloper.App.ViewModels;

public sealed partial class UiText
{
    public string WebStackModules => IsCzech ? "Webový a databázový stack" : "Web and database stack";

    public string DevelopmentModules => IsCzech ? "Vývojové nástroje" : "Development tools";

    public string AutomationModules => IsCzech ? "Automatizace prohlížeče" : "Browser automation";

    public string RuntimePackageName(RuntimePackageKind kind) => kind switch
    {
        RuntimePackageKind.Apache => "Apache HTTP Server",
        RuntimePackageKind.Php => "PHP",
        RuntimePackageKind.Database => IsCzech ? "Databáze" : "Database",
        RuntimePackageKind.Selenium => "Selenium",
        RuntimePackageKind.Composer => "Composer",
        RuntimePackageKind.Node => "Node.js + npm",
        RuntimePackageKind.Python => "Python",
        RuntimePackageKind.Editor => IsCzech ? "Editor" : "Editor",
        RuntimePackageKind.PhpMyAdmin => "phpMyAdmin",
        RuntimePackageKind.SeleniumChromeEnvironment => "Chrome for Testing + ChromeDriver",
        RuntimePackageKind.SeleniumFirefoxEnvironment => "Mozilla Firefox + geckodriver",
        _ => kind.ToString()
    };

    public string RuntimePackageDescription(RuntimePackageKind kind) => kind switch
    {
        RuntimePackageKind.Apache => IsCzech ? "Lokální webový server pro statické projekty; pokud je nainstalované PHP, připojí se automaticky přes FastCGI." : "Local web server for static projects; when PHP is installed, it is attached automatically through FastCGI.",
        RuntimePackageKind.Php => IsCzech ? "Přenosný PHP runtime pro Apache a Composer." : "Portable PHP runtime for Apache and Composer.",
        RuntimePackageKind.Database => IsCzech ? "Přenosný MariaDB server a lokální databáze." : "Portable MariaDB server and local databases.",
        RuntimePackageKind.Selenium => IsCzech ? "Selenium Server a vlastní portable Java runtime; spravovaný browser si vyberete zvlášť." : "Selenium Server and its portable Java runtime; choose a managed browser separately.",
        RuntimePackageKind.Composer => IsCzech ? "Správa PHP knihoven; chybějící PHP se doplní automaticky." : "PHP dependency management; missing PHP is added automatically.",
        RuntimePackageKind.Node => IsCzech ? "Přenosný Node.js runtime s npm pro projektové JavaScriptové balíčky." : "Portable Node.js runtime with npm for project JavaScript packages.",
        RuntimePackageKind.Python => IsCzech ? "Přenosný Python s projektovou správou knihoven." : "Portable Python with project package management.",
        RuntimePackageKind.Editor => IsCzech ? "Lehký portable Notepad++ propojený se správcem souborů." : "Lightweight portable Notepad++ integrated with the file manager.",
        RuntimePackageKind.PhpMyAdmin => IsCzech ? "Webová správa databází včetně Apache, PHP a MariaDB." : "Web database administration including Apache, PHP, and MariaDB.",
        RuntimePackageKind.SeleniumChromeEnvironment => IsCzech ? "Spravovaný a verzově shodný balíček browseru a driveru pro čisté automatizační relace." : "Managed, version-matched browser and driver bundle for clean automation sessions.",
        RuntimePackageKind.SeleniumFirefoxEnvironment => IsCzech ? "Doporučený spravovaný Firefox a geckodriver; vhodný také pro přenosné přihlašovací profily." : "Recommended managed Firefox and geckodriver; also suitable for portable signed-in profiles.",
        _ => string.Empty
    };

    public string DownloadAndInstall => IsCzech ? "Stáhnout a nainstalovat" : "Download and install";

    public string Installed => IsCzech ? "Nainstalováno" : "Installed";

    public string PackageInstalledAndVerified => IsCzech ? "Nainstalováno a ověřeno" : "Installed and verified";

    public string PackageMissingComponents => IsCzech ? "Připraveno ke stažení" : "Ready to download";

    public string PackageInstallProgress(RuntimePackageInstallProgress progress) => progress.Stage switch
    {
        RuntimePackageInstallStage.Preparing => IsCzech ? "Připravuji instalaci…" : "Preparing installation…",
        RuntimePackageInstallStage.Downloading => IsCzech ? $"Stahuji {progress.ComponentName}…" : $"Downloading {progress.ComponentName}…",
        RuntimePackageInstallStage.Verifying => IsCzech ? $"Ověřuji {progress.ComponentName}…" : $"Verifying {progress.ComponentName}…",
        RuntimePackageInstallStage.Extracting => IsCzech ? $"Rozbaluji {progress.ComponentName}…" : $"Extracting {progress.ComponentName}…",
        RuntimePackageInstallStage.Installing => IsCzech ? "Dokončuji portable instalaci…" : "Finishing portable installation…",
        RuntimePackageInstallStage.Completed => PackageInstalledAndVerified,
        _ => PackageMissingComponents
    };

    public string PackageDownloadSize(RuntimePackageInstallProgress progress)
    {
        if (progress.Stage != RuntimePackageInstallStage.Downloading || progress.BytesReceived <= 0)
        {
            return string.Empty;
        }

        var current = FormatDownloadBytes(progress.BytesReceived);
        var size = progress.TotalBytes is > 0
            ? $"{current} / {FormatDownloadBytes(progress.TotalBytes.Value)}"
            : current;
        return progress.ComponentCount > 1
            ? $"{size}  ·  {progress.ComponentIndex}/{progress.ComponentCount}"
            : size;
    }

    private static string FormatDownloadBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        double value = bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value:0.#} {units[unit]}";
    }

    public string PackageInstallFailed(string detail) => IsCzech
        ? $"Instalace modulu selhala: {detail}"
        : $"Module installation failed: {detail}";

    public string PackageInstallSucceeded(string name) => IsCzech
        ? $"Modul {name} je nainstalovaný a připravený."
        : $"Module {name} is installed and ready.";

}
