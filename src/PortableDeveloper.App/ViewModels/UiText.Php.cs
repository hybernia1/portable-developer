using PortableDeveloper.Domain.Processes;

namespace PortableDeveloper.App.ViewModels;

public sealed partial class UiText
{
    public string PhpSettings => IsCzech ? "Nastavení php.ini" : "php.ini settings";

    public string MemoryLimit => "memory_limit (MB)";

    public string UploadLimit => "upload_max_filesize (MB)";

    public string PostLimit => "post_max_size (MB)";

    public string ExecutionTime => IsCzech ? "max_execution_time (sekundy)" : "max_execution_time (seconds)";

    public string MaximumInputVariables => "max_input_vars";

    public string DisplayErrors => IsCzech ? "Zobrazovat chyby ve výstupu" : "Display errors in output";

    public string DisplayErrorsHelp => IsCzech
        ? "Chyby se vždy zapisují do instances/default/logs/php-error.log. Zobrazení ve stránce je vhodné jen pro lokální vývoj."
        : "Errors are always logged to instances/default/logs/php-error.log. Displaying them in the page is suitable only for local development.";

    public string PhpExtensions => IsCzech ? "PHP rozšíření" : "PHP extensions";

    public string RequiredPhpExtensions => IsCzech
        ? "Povinná rozšíření mbstring, mysqli, openssl a zip jsou vždy aktivní."
        : "Required extensions mbstring, mysqli, openssl, and zip are always enabled.";

    public string SavePhpSettings => IsCzech ? "Uložit PHP nastavení" : "Save PHP settings";

    public string SaveAndRestartPhp => IsCzech ? "Uložit a restartovat Apache" : "Save and restart Apache";

    public string ResetDefaults => IsCzech ? "Výchozí hodnoty" : "Default values";

    public string PhpSettingsInvalid => IsCzech
        ? "Zkontrolujte rozsahy: paměť 32–8192 MB, upload 1–2048 MB, POST 1–4096 MB, timeout 0–3600 s a max_input_vars 100–100000. POST limit nesmí být menší než upload."
        : "Check the ranges: memory 32–8192 MB, upload 1–2048 MB, POST 1–4096 MB, timeout 0–3600 s, and max_input_vars 100–100000. The POST limit cannot be smaller than the upload limit.";

    public string PhpSettingsSaved(ManagedProcessState apacheState) => apacheState == ManagedProcessState.Running
        ? IsCzech
            ? "PHP nastavení bylo uloženo a Apache byl restartován."
            : "PHP settings were saved and Apache was restarted."
        : IsCzech
            ? "PHP nastavení bylo uloženo a použije se při příštím startu Apache."
            : "PHP settings were saved and will be used the next time Apache starts.";

    public string PhpSettingsSaveFailed(string detail) => IsCzech
        ? $"PHP nastavení se nepodařilo uložit: {detail}"
        : $"PHP settings could not be saved: {detail}";

    public string PhpDefaultsPrepared => IsCzech
        ? "Výchozí hodnoty jsou připravené ve formuláři. Potvrďte je tlačítkem Uložit PHP nastavení."
        : "Default values are ready in the form. Confirm them with Save PHP settings.";

    public string ProjectName => IsCzech ? "Název projektu" : "Project name";

    public string CreateProject => IsCzech ? "Vytvořit projekt" : "Create project";

    public string ActiveProject => IsCzech ? "Aktivní projekt nástrojů" : "Active tools project";

    public string ActiveProjectBadge => IsCzech ? "Aktivní" : "Active";

    public string DefaultProjectName => IsCzech ? "Výchozí" : "Default";

    public string Enabled => IsCzech ? "povoleno" : "enabled";

    public string Disabled => IsCzech ? "vypnuto" : "disabled";

    public string EnableHtaccess => IsCzech ? "Povolit .htaccess" : "Enable .htaccess";

    public string DisableHtaccess => IsCzech ? "Vypnout .htaccess" : "Disable .htaccess";

    public string EnableInApache => IsCzech ? "Zapnout v Apache" : "Enable in Apache";

    public string DisableInApache => IsCzech ? "Vypnout v Apache" : "Disable in Apache";

    public string ProjectOperationFailed(string detail) => IsCzech
        ? $"Operace s projektem selhala: {detail}"
        : $"Project operation failed: {detail}";

    public string ProjectChangeBusy => IsCzech
        ? "Projekt nelze přepnout, vytvořit ani odebrat během právě běžící operace správce balíčků nebo terminálu."
        : "A project cannot be selected, created, or removed while a package manager or terminal operation is running.";

}
