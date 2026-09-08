using PortableDeveloper.Application.Selenium;
using PortableDeveloper.Domain.Processes;

namespace PortableDeveloper.App.ViewModels;

public sealed partial class UiText
{
    public string SeleniumSettings => IsCzech ? "Nastavení serveru" : "Server settings";

    public string SeleniumFirewallNoticeTitle => IsCzech ? "Lokální Selenium a Windows Firewall" : "Local Selenium and Windows Firewall";

    public string SeleniumFirewallNotice => IsCzech
        ? "Selenium i spravované browsery naslouchají pouze na 127.0.0.1. Windows může při prvním startu přesto zobrazit dotaz na povolení Java runtime ve firewallu. Pro Portable Developer není síťová výjimka potřeba — zvolte Zrušit. Aplikace firewall sama nemění. Toto upozornění se zobrazí jen jednou."
        : "Selenium and its managed browsers listen only on 127.0.0.1. Windows may still ask whether to allow the Java runtime through the firewall on first start. Portable Developer does not need a network exception—choose Cancel. The application never changes the firewall itself. This notice is shown only once.";

    public string ContinueSeleniumStart => IsCzech ? "Rozumím, spustit" : "Understood, start";

    public string MaximumSessions => IsCzech ? "Maximum souběžných relací" : "Maximum concurrent sessions";

    public string SessionTimeout => IsCzech ? "Limit neaktivity relace (sekundy)" : "Session inactivity timeout (seconds)";

    public string EnableSeleniumDownloads => IsCzech ? "Povolit stahování souborů" : "Allow file downloads";

    public string SaveSeleniumSettings => IsCzech ? "Uložit nastavení" : "Save settings";

    public string SaveAndRestartSelenium => IsCzech
        ? "Uložit a restartovat Selenium"
        : "Save and restart Selenium";

    public string RestartingSeleniumService => IsCzech
        ? "Restartuji Selenium a ukončuji jeho relace…"
        : "Restarting Selenium and terminating its sessions…";

    public string SeleniumSettingsSaved(ManagedProcessState seleniumState) => seleniumState == ManagedProcessState.Running
        ? IsCzech
            ? "Nastavení Selenium bylo uloženo a server restartován."
            : "Selenium settings were saved and the server was restarted."
        : IsCzech
            ? "Nastavení Selenium bylo uloženo a použije se při příštím startu."
            : "Selenium settings were saved and will be used on the next start.";

    public string SeleniumSettingsInvalid => IsCzech
        ? "Zadejte port 1024–65535, 1–32 relací a timeout 30–86400 sekund."
        : "Enter port 1024–65535, 1–32 sessions, and a timeout of 30–86400 seconds.";

    public string SeleniumDrivers => IsCzech ? "Browser prostředí" : "Browser environments";

    public string SeleniumDriverCatalog => IsCzech ? "Katalog browser prostředí" : "Browser environment catalog";

    public string SeleniumProfiles => IsCzech ? "Profily" : "Profiles";

    public string SeleniumBrowserProfiles => IsCzech ? "Browser profily" : "Browser profiles";

    public string SeleniumCookieVaults => IsCzech ? "Cookie vault" : "Cookie vault";

    public string CookieVaultManagement => IsCzech ? "Spravované cookie vaulty" : "Managed cookie vaults";

    public string CookieVaultHelp => IsCzech
        ? "Import přijme JSON export cookies, ponechá jen údaje potřebné pro Selenium a vyřadí prošlé či neplatné položky. Původní export zůstane beze změny."
        : "Import accepts a JSON cookie export, keeps only fields required by Selenium, and discards expired or invalid items. The original export remains unchanged.";

    public string CookieVaultName => IsCzech ? "Název vaultu" : "Vault name";

    public string CookieVaultNameRequired => IsCzech
        ? "Zadejte název vaultu (1 až 80 znaků)."
        : "Enter a vault name (1 to 80 characters).";

    public string CookieExportFile => IsCzech ? "JSON soubor s cookies" : "Cookie JSON file";

    public string ChooseCookieFile => IsCzech ? "Vybrat soubor…" : "Choose file…";

    public string NoCookieFileSelected => IsCzech ? "Není vybraný žádný soubor." : "No file selected.";

    public string CookieVaultAutomaticProtectionHelp => IsCzech
        ? "Aplikace vault automaticky zašifruje klíčem uloženým uvnitř portable složky. Není potřeba žádné heslo ani odemykání."
        : "The app automatically encrypts the vault with a key stored inside the portable folder. No password or unlocking is required.";

    public string AddCookieVault => IsCzech ? "Přidat vault" : "Add vault";

    public string CookieVaultImported(string name, int skipped) => IsCzech
        ? $"Vault {name} byl vytvořen. Vyřazené nebo duplicitní cookies: {skipped}."
        : $"Vault {name} was created. Discarded or duplicate cookies: {skipped}.";

    public string CookieVaultImportFailed(string detail) => IsCzech
        ? $"Import cookie vaultu selhal: {detail}"
        : $"Cookie vault import failed: {detail}";

    public string CookieVaultCount(int count) => IsCzech ? $"Vaulty: {count}" : $"Vaults: {count}";

    public string CookieCount(int count) => IsCzech ? $"Cookies: {count}" : $"Cookies: {count}";

    public string NoCookieDomains => IsCzech ? "Žádné domény" : "No domains";

    public string NoCookieVaults => IsCzech ? "Zatím není vytvořený žádný cookie vault." : "No cookie vault has been created yet.";

    public string CookieVaultReady => IsCzech
        ? "Připraveno — Selenium data rozšifruje pouze při vytváření relace"
        : "Ready — Selenium decrypts the data only while creating a session";

    public string DamagedVault(string detail) => IsCzech ? $"Poškozený vault: {detail}" : $"Damaged vault: {detail}";

    public string RemoveCookieVaultTitle => IsCzech ? "Odstranění cookie vaultu" : "Remove cookie vault";

    public string RemoveCookieVaultQuestion(string name) => IsCzech
        ? $"Opravdu trvale odstranit zašifrovaný vault {name}? Bez zálohy jej nelze obnovit."
        : $"Permanently remove encrypted vault {name}? It cannot be recovered without a backup.";

    public string CookieVaultRemoved => IsCzech ? "Cookie vault byl odstraněn." : "Cookie vault was removed.";

    public string CookieVaultCapabilityHelp => IsCzech
        ? "Do capabilities relace přidejte portable:vault s uvedeným ID. Aplikace vault použije automaticky."
        : "Add portable:vault with the shown ID to session capabilities. The app uses the vault automatically.";

    public string SeleniumProfileMasters => IsCzech ? "Master profily" : "Master profiles";

    public string SeleniumProfileManagement => IsCzech ? "Přihlašovací profily" : "Signed-in profiles";

    public string SeleniumProfilesHelp => IsCzech
        ? "Profil vznikne pouze ve spravovaném browseru aplikace. Přihlaste se, browser zavřete a aplikace uloží neměnný master; každá relace dostane dočasnou kopii, která se po ukončení smaže. Pro přenos přihlášení doporučujeme Firefox."
        : "The profile is created only in an app-managed browser. Sign in, close the browser, and the app seals an immutable master; each session gets a temporary copy that is removed afterwards. Firefox is recommended for portable sign-in state.";

    public string ProfileName => IsCzech ? "Název profilu" : "Profile name";

    public string ProfileNameRequired => IsCzech
        ? "Nejdřív zadejte název profilu (1 až 80 znaků)."
        : "Enter a profile name first (1 to 80 characters).";

    public string BrowserEnvironment => IsCzech ? "Spravovaný prohlížeč" : "Managed browser";

    public string CreateCleanMaster => IsCzech ? "Vytvořit přihlašovací profil" : "Create signed-in profile";

    public string AddSeleniumProfile => IsCzech ? "Přidat profil" : "Add profile";

    public string CreateCleanMasterHelp => IsCzech
        ? "Otevře nový dočasný profil uvnitř aplikace. Přihlaste se pouze k webům, které chcete automatizovat, a browser zavřete; profil se ověří a uloží jako neměnný master."
        : "Opens a fresh temporary profile inside the app. Sign in only to sites you want to automate and close the browser; the profile is verified and stored as an immutable master.";

    public string SelectBrowserEnvironment => IsCzech ? "Nejdřív vyberte dostupný prohlížeč." : "Select an available browser first.";

    public string UnsupportedBrowserEnvironment => IsCzech ? "Vybraný typ prohlížeče není podporovaný." : "The selected browser type is not supported.";

    public string ConfigureBrowserAndClose => IsCzech ? "Přihlaste se ve spravovaném prohlížeči a potom jej zavřete…" : "Sign in using the managed browser and then close it…";

    public string SeleniumProfileWaiting => IsCzech
        ? "Prohlížeč běží. Přihlaste účet, který chcete v profilu používat, a potom zavřete všechna jeho okna."
        : "The browser is running. Sign in to the account this profile should use, then close all of its windows.";

    public string SeleniumProfileSealing => IsCzech
        ? "Prohlížeč byl zavřen. Profil se kopíruje, čistí a ověřuje…"
        : "The browser is closed. Copying, cleaning, and verifying the profile…";

    public string SeleniumProfileCleaning => IsCzech
        ? "Dokončuji profil a odstraňuji pracovní soubory…"
        : "Finishing the profile and removing working files…";

    public string BrowserCouldNotStart => IsCzech ? "Prohlížeč se nepodařilo spustit." : "The browser could not be started.";

    public string NoSeleniumProfiles => IsCzech ? "Zatím není vytvořený žádný přihlašovací profil." : "No signed-in profile has been created yet.";

    public string SeleniumProfileCount(int count) => IsCzech ? $"Master profily: {count}" : $"Master profiles: {count}";

    public string VerifiedProfile => IsCzech ? "Ověřený neměnný master" : "Verified immutable master";

    public string ProfileBrowserUnavailable => IsCzech
        ? "Master je ověřený, ale kompatibilní browser prostředí není připravené"
        : "Master is verified, but no compatible browser environment is ready";

    public string DamagedProfile(string detail) => IsCzech ? $"Poškozený: {detail}" : $"Damaged: {detail}";

    public string SeleniumProfileBrowserLabel(SeleniumProfileBrowser browser) => browser switch
    {
        SeleniumProfileBrowser.Edge => "Microsoft Edge",
        SeleniumProfileBrowser.Chrome => "Google Chrome",
        SeleniumProfileBrowser.Firefox => "Mozilla Firefox",
        _ => browser.ToString()
    };

    public string SeleniumProfileCreated(string name) => IsCzech ? $"Profil {name} byl bezpečně vytvořen." : $"Profile {name} was created safely.";

    public string EditSeleniumProfile => IsCzech ? "Upravit profil" : "Edit profile";

    public string EditSeleniumProfileTitle => IsCzech ? "Úprava master profilu" : "Edit master profile";

    public string EditSeleniumProfileQuestion(string name) => IsCzech
        ? $"Otevřít pracovní kopii profilu {name}? Po zavření browseru aplikace kopii ověří a bezpečně jí nahradí současný master. ID profilu zůstane stejné."
        : $"Open a working copy of profile {name}? After the browser closes, the app verifies it and safely replaces the current master. The profile ID stays unchanged.";

    public string SeleniumProfilePreparingEdit => IsCzech
        ? "Připravuji zapisovatelnou pracovní kopii master profilu…"
        : "Preparing a writable working copy of the master profile…";

    public string SeleniumProfileEditing => IsCzech
        ? "Profil je otevřený pro úpravy. Po dokončení zavřete všechna okna browseru."
        : "The profile is open for editing. Close all browser windows when finished.";

    public string SeleniumProfileUpdated(string name) => IsCzech
        ? $"Master profil {name} byl bezpečně aktualizován; jeho ID zůstalo stejné."
        : $"Master profile {name} was safely updated; its ID stayed unchanged.";

    public string SeleniumProfileUpdateFailed(string detail) => IsCzech
        ? $"Úprava profilu selhala: {detail}"
        : $"Profile update failed: {detail}";

    public string CopyId => IsCzech ? "Kopírovat ID" : "Copy ID";

    public string ProfileIdCopied => IsCzech ? "ID profilu bylo zkopírováno." : "Profile ID was copied.";

    public string CookieVaultIdCopied => IsCzech ? "ID cookie vaultu bylo zkopírováno." : "Cookie vault ID was copied.";

    public string CopyIdFailed(string detail) => IsCzech
        ? $"Kopírování ID selhalo: {detail}"
        : $"Copying the ID failed: {detail}";

    public string SeleniumProfileCreateFailed(string detail) => IsCzech ? $"Vytvoření profilu selhalo: {detail}" : $"Profile creation failed: {detail}";

    public string RemoveSeleniumProfileTitle => IsCzech ? "Odebrání master profilu" : "Remove master profile";

    public string RemoveSeleniumProfileQuestion(string name) => IsCzech
        ? $"Opravdu odebrat master profil {name}? Zdrojový profil mimo aplikaci zůstane beze změny."
        : $"Remove master profile {name}? The original profile outside the application will remain unchanged.";

    public string SeleniumProfileRemoved => IsCzech ? "Master profil byl odebrán." : "The master profile was removed.";

    public string ReloadDrivers => IsCzech ? "Obnovit browsery" : "Refresh browsers";

    public string SeleniumSessions => IsCzech ? "Běžící relace" : "Running sessions";

    public string SeleniumSessionCount(int count, int maximum) => IsCzech
        ? $"Aktivní relace: {count} / {maximum}"
        : $"Active sessions: {count} / {maximum}";

    public string NoSeleniumSessions => IsCzech ? "Momentálně neběží žádná relace." : "No sessions are currently running.";

    public string OpenSeleniumHub => IsCzech ? "Otevřít Hub" : "Open Hub";

    public string TerminateSession => IsCzech ? "Ukončit relaci" : "Terminate session";

    public string TerminateSessionQuestion => IsCzech
        ? "Opravdu ukončit vybranou Selenium relaci? Prohlížeč a jeho rozpracovaný stav se zavřou."
        : "Terminate the selected Selenium session? Its browser and in-progress state will be closed.";

    public string TerminateSessionTitle => IsCzech ? "Ukončení Selenium relace" : "Terminate Selenium session";

    public string TerminatingSession => IsCzech ? "Ukončuji Selenium relaci…" : "Terminating Selenium session…";

    public string SeleniumSessionTerminated => IsCzech ? "Selenium relace byla ukončena." : "The Selenium session was terminated.";

    public string SeleniumSessionsFailed(string detail) => IsCzech
        ? $"Relace Selenium se nepodařilo načíst: {detail}"
        : $"Selenium sessions could not be loaded: {detail}";

    public string SeleniumOperationFailed(string detail) => IsCzech
        ? $"Operace Selenium selhala: {detail}"
        : $"Selenium operation failed: {detail}";

    public string Browser => IsCzech ? "Prohlížeč" : "Browser";

    public string Platform => IsCzech ? "Platforma" : "Platform";

    public string Started => IsCzech ? "Spuštěno" : "Started";

    public string Duration => IsCzech ? "Doba běhu" : "Duration";

    public string SeleniumAction(ManagedProcessState state) => state switch
    {
        ManagedProcessState.Running => IsCzech ? "Zastavit Selenium" : "Stop Selenium",
        ManagedProcessState.Starting => IsCzech ? "Spouštím…" : "Starting…",
        ManagedProcessState.Stopping => IsCzech ? "Zastavuji…" : "Stopping…",
        ManagedProcessState.Failed => IsCzech ? "Zkusit znovu" : "Try again",
        _ => IsCzech ? "Spustit Selenium" : "Start Selenium"
    };

    public string SeleniumRuntimeDetail(string version, ManagedProcessState state, int port, int driverCount) => state switch
    {
        ManagedProcessState.Running => IsCzech
            ? $"Verze {version} naslouchá na portu {port}; připravené browsery: {driverCount}."
            : $"Version {version} is listening on port {port}; ready browsers: {driverCount}.",
        ManagedProcessState.Starting => IsCzech ? "Spouštím lokální Standalone Grid…" : "Starting the local Standalone Grid…",
        ManagedProcessState.Stopping => IsCzech ? "Ukončuji Grid a jeho relace…" : "Stopping the Grid and its sessions…",
        _ when driverCount == 0 => IsCzech
            ? $"Verze {version} je ověřená. Před spuštěním stáhněte na kartě Browser prostředí alespoň jeden spravovaný browser."
            : $"Version {version} is verified. Download at least one managed browser on the Browser environments tab before starting.",
        _ => IsCzech
            ? $"Verze {version} je ověřená; připravené browsery: {driverCount}."
            : $"Version {version} is verified; ready browsers: {driverCount}."
    };

}
