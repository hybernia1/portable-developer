using PortableDeveloper.Application.Storage;

namespace PortableDeveloper.App.ViewModels;

public sealed partial class UiText
{
    public string Language => IsCzech ? "Jazyk rozhraní" : "Interface language";

    public string CacheManagement => IsCzech ? "Správa cache" : "Cache management";

    public string CacheManagementHelp => IsCzech
        ? "Instalační archivy jsou po úspěšné instalaci automaticky odstraněny. Zde lze bezpečně vyčistit pouze obnovitelné cache; nainstalované moduly ani projektová data se nemažou."
        : "Installation archives are removed automatically after a successful installation. Only reproducible caches can be cleared here; installed modules and project data are never deleted.";

    public string RuntimePackageCache => IsCzech ? "Stažené instalační balíčky" : "Downloaded installation packages";

    public string ComposerCache => "Composer cache";

    public string NpmCache => "npm cache";

    public string PipCache => "pip cache";

    public string TotalCache => IsCzech ? "Cache celkem" : "Total cache";

    public string ClearCache => IsCzech ? "Vyčistit" : "Clear";

    public string ClearAllCaches => IsCzech ? "Vyčistit vše" : "Clear all";

    public string ClearAllCachesQuestion => IsCzech
        ? "Vyčistit všechny obnovitelné cache? Nainstalované moduly ani projektová data se nesmažou."
        : "Clear every reproducible cache? Installed modules and project data will not be deleted.";

    public string AllCachesCleared(string size) => IsCzech
        ? $"Všechny cache byly vyčištěny. Uvolněno: {size}."
        : $"All caches were cleared. Reclaimed: {size}.";

    public string RefreshStorage => IsCzech ? "Přepočítat" : "Refresh";

    public string ProtectedStorage => IsCzech ? "Chráněná data" : "Protected storage";

    public string ProtectedStorageHelp => IsCzech
        ? "Tyto položky jsou pouze informativní. Automatické čištění se jich nikdy nedotkne."
        : "These values are informational only. Automatic cleanup never touches them.";

    public string InstalledRuntimes => IsCzech ? "Nainstalované moduly, drivery a nástroje" : "Installed modules, drivers, and tools";

    public string PersistentProjectData => IsCzech ? "Instance, projekty a profily" : "Instances, projects, and profiles";

    public string MeasuringStorage => IsCzech ? "Počítám využití úložiště…" : "Measuring storage usage…";

    public string StorageMeasured => IsCzech ? "Využití úložiště je aktuální." : "Storage usage is up to date.";

    public string StorageBusy => IsCzech
        ? "Cache nelze čistit během instalace balíčku nebo příkazu v terminálu. Počkejte na dokončení operace."
        : "Caches cannot be cleared while a package installation or terminal command is running. Wait for the operation to finish.";

    public string StorageMeasureFailed(string detail) => IsCzech
        ? $"Využití úložiště se nepodařilo zjistit: {detail}"
        : $"Storage usage could not be measured: {detail}";

    public string ClearCacheTitle => IsCzech ? "Vyčištění cache" : "Clear cache";

    public string ClearCacheQuestion(string cache) => IsCzech
        ? $"Opravdu vyčistit {cache}? Data lze znovu stáhnout a projektové soubory zůstanou beze změny."
        : $"Clear {cache}? The data can be downloaded again and project files will remain unchanged.";

    public string ClearingCache(string cache) => IsCzech ? $"Čistím {cache}…" : $"Clearing {cache}…";

    public string CacheCleared(string cache, string size) => IsCzech
        ? $"{cache} byla vyčištěna; uvolněno {size}."
        : $"{cache} was cleared; {size} released.";

    public string CacheClearFailed(string cache, string detail) => IsCzech
        ? $"{cache} se nepodařilo vyčistit: {detail}"
        : $"{cache} could not be cleared: {detail}";

    public string StorageCacheName(StorageCacheKind cache) => cache switch
    {
        StorageCacheKind.RuntimePackages => RuntimePackageCache,
        StorageCacheKind.Composer => ComposerCache,
        StorageCacheKind.Npm => NpmCache,
        StorageCacheKind.Pip => PipCache,
        _ => IsCzech ? "cache" : "cache"
    };

    public string ApplicationVersion => IsCzech ? "Verze aplikace" : "Application version";

}
