using PortableDeveloper.Domain.Processes;

namespace PortableDeveloper.App.ViewModels;

public sealed partial class UiText
{
    public string ConnectionDetails => IsCzech ? "Připojení" : "Connection";

    public string Host => "Host";

    public string Port => IsCzech ? "Port" : "Port";

    public string User => IsCzech ? "Uživatel" : "User";

    public string Password => IsCzech ? "Heslo" : "Password";

    public string RootPasswordSecurity => IsCzech ? "Zabezpečení účtu root" : "Root account security";

    public string NewPassword => IsCzech ? "Nové heslo" : "New password";

    public string ConfirmPassword => IsCzech ? "Potvrzení hesla" : "Confirm password";

    public string SetPassword => IsCzech ? "Nastavit heslo" : "Set password";

    public string ChangePassword => IsCzech ? "Změnit heslo" : "Change password";

    public string PasswordConfigured => IsCzech ? "Heslo je nastavené" : "Password is configured";

    public string NoPasswordConfigured => IsCzech ? "Výchozí stav: bez hesla" : "Default state: no password";

    public string PasswordMismatch => IsCzech ? "Zadaná hesla se neshodují." : "The entered passwords do not match.";

    public string PasswordChanging => IsCzech ? "Měním heslo účtu root…" : "Changing the root password…";

    public string PasswordChanged => IsCzech
        ? "Heslo účtu root bylo změněno a portable připojení bylo aktualizováno."
        : "The root password was changed and the portable connection was updated.";

    public string PasswordChangeFailed(string detail) => IsCzech
        ? $"Heslo se nepodařilo změnit: {detail}"
        : $"The password could not be changed: {detail}";

    public string PasswordGuidance => IsCzech
        ? "Použijte alespoň 8 znaků. Heslo se nezobrazuje v UI, argumentech procesů ani logu."
        : "Use at least 8 characters. The password is never shown in the UI, process arguments, or logs.";

    public string PhpMyAdminDescription => IsCzech
        ? "Webová správa databází přes lokální Apache a PHP. Přihlaste se jako root aktuálním heslem."
        : "Web database administration through local Apache and PHP. Sign in as root with the current password.";

    public string OpenPhpMyAdmin => IsCzech ? "Otevřít phpMyAdmin" : "Open phpMyAdmin";

    public string OpeningPhpMyAdmin => IsCzech ? "Otevírám phpMyAdmin…" : "Opening phpMyAdmin…";

    public string PhpMyAdminReady => IsCzech
        ? "Apache i MariaDB běží. phpMyAdmin je připravený."
        : "Apache and MariaDB are running. phpMyAdmin is ready.";

    public string PhpMyAdminNeedsWeb => IsCzech
        ? "Nejprve spusťte Apache na stránce Apache."
        : "Start Apache on the Apache page first.";

    public string PhpMyAdminNeedsPhp => IsCzech
        ? "Pro phpMyAdmin nejprve nainstalujte PHP. Samotný Apache může dál běžet jako statický server."
        : "Install PHP before using phpMyAdmin. Apache can continue running as a static server without it.";

    public string PhpMyAdminNeedsDatabase => IsCzech
        ? "Nejprve spusťte MariaDB."
        : "Start MariaDB first.";

    public string PhpMyAdminNeedsBoth => IsCzech
        ? "phpMyAdmin vyžaduje spuštěný Apache i MariaDB."
        : "phpMyAdmin requires both Apache and MariaDB to be running.";

    public string Version => IsCzech ? "Verze" : "Version";

    public string PhpIni => "php.ini";

    public string DocumentRoot => "Document root";

    public string RootAccountNote => IsCzech
        ? "Výchozí účet root je bez hesla a dostupný pouze na 127.0.0.1. Vlastní heslo můžete nastavit níže; tato instance není určená pro produkci."
        : "The root account has no password by default and is only available at 127.0.0.1. You can set a password below; this instance is not intended for production.";

    public string CreateDatabase => IsCzech ? "Vytvořit databázi" : "Create database";

    public string NewDatabaseName => IsCzech ? "Název nové databáze" : "New database name";

    public string DatabaseOverview => IsCzech ? "Přehled databází" : "Database overview";

    public string ApproximateSize => IsCzech ? "Orientační velikost" : "Approximate size";

    public string Refresh => IsCzech ? "Obnovit" : "Refresh";

    public string ManageDatabase => IsCzech ? "Spravovat" : "Manage";

    public string DeleteDatabase => IsCzech ? "Smazat" : "Delete";

    public string DeleteDatabaseTitle => IsCzech ? "Smazat databázi" : "Delete database";

    public string DeleteDatabaseQuestion(string name) => IsCzech
        ? $"Opravdu chcete trvale smazat databázi {name} včetně všech jejích tabulek a dat?"
        : $"Permanently delete the {name} database, including all its tables and data?";

    public string DeletingDatabase(string name) => IsCzech
        ? $"Mažu databázi {name}…"
        : $"Deleting database {name}…";

    public string DatabaseDeleted(string name) => IsCzech
        ? $"Databáze {name} byla smazána."
        : $"Database {name} was deleted.";

    public string DatabaseDeleteFailed(string detail) => IsCzech
        ? $"Databázi se nepodařilo smazat: {detail}"
        : $"The database could not be deleted: {detail}";

    public string DefaultDatabaseCannotBeDeleted => IsCzech
        ? "Výchozí databázi portable_dev nelze smazat."
        : "The default portable_dev database cannot be deleted.";

    public string DefaultDatabase => IsCzech ? "Výchozí databáze" : "Default database";

    public string DatabaseCount(int count) => IsCzech ? $"Databáze: {count}" : $"Databases: {count}";

    public string CreatingDatabase => IsCzech ? "Vytvářím databázi…" : "Creating database…";

    public string DatabaseCreated(string name) => IsCzech
        ? $"Databáze {name} byla vytvořena."
        : $"Database {name} was created.";

    public string DatabaseCreateFailed(string detail) => IsCzech
        ? $"Databázi se nepodařilo vytvořit: {detail}"
        : $"The database could not be created: {detail}";

    public string DatabaseOverviewFailed(string detail) => IsCzech
        ? $"Přehled databází se nepodařilo načíst: {detail}"
        : $"The database overview could not be loaded: {detail}";

    public string MariaDbReady => IsCzech
        ? "MariaDB je připravená a výchozí databáze portable_dev je dostupná."
        : "MariaDB is ready and the default portable_dev database is available.";

    public string MariaDbPreparedStopped => IsCzech
        ? "MariaDB a výchozí databáze portable_dev jsou připravené. Server zůstává zastavený, dokud jej ručně nespustíte."
        : "MariaDB and the default portable_dev database are ready. The server remains stopped until you start it.";

    public string MariaDbStarting => IsCzech ? "Spouštím MariaDB…" : "Starting MariaDB…";

    public string MariaDbStopping => IsCzech ? "Zastavuji MariaDB…" : "Stopping MariaDB…";

    public string MariaDbAction(ManagedProcessState state) => state switch
    {
        ManagedProcessState.Running => IsCzech ? "Zastavit MariaDB" : "Stop MariaDB",
        ManagedProcessState.Starting => IsCzech ? "Spouštím…" : "Starting…",
        ManagedProcessState.Stopping => IsCzech ? "Zastavuji…" : "Stopping…",
        ManagedProcessState.Failed => IsCzech ? "Zkusit znovu" : "Try again",
        _ => IsCzech ? "Spustit MariaDB" : "Start MariaDB"
    };

    public string MariaDbRuntimeDetail(string version, ManagedProcessState state, int port) => state switch
    {
        ManagedProcessState.Running => RunningModule(version, port),
        ManagedProcessState.Starting => IsCzech ? "Server se spouští pouze na localhostu." : "The server is starting on localhost only.",
        ManagedProcessState.Stopping => IsCzech ? "Server se bezpečně ukončuje." : "The server is shutting down safely.",
        _ => VerifiedModule(version)
    };

}
