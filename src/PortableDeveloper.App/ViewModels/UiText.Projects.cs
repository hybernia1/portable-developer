using PortableDeveloper.Application.Projects;

namespace PortableDeveloper.App.ViewModels;

public sealed partial class UiText
{
    public string TechnicalDetails => IsCzech ? "Technické informace" : "Technical information";

    public string ServiceControl => IsCzech ? "Ovládání služby" : "Service control";

    public string CurrentConfiguration => IsCzech ? "Aktuální konfigurace" : "Current configuration";

    public string Planned => IsCzech ? "Plánováno" : "Planned";

    public string ManageProjects => IsCzech ? "Spravovat" : "Manage";

    public string ProjectsTab => IsCzech ? "Projekty" : "Projects";

    public string CreateProjectTab => IsCzech ? "Nový projekt" : "New project";

    public string AddProjectTab => IsCzech ? "Přidat projekt" : "Add project";

    public string ProjectCapabilities => IsCzech ? "Schopnosti" : "Capabilities";

    public string ProjectTools => IsCzech ? "Nástroje projektu" : "Project tools";

    public string ProjectManagement => IsCzech ? "Správa projektu" : "Project management";

    public string WebSupport => IsCzech ? "Webová podpora" : "Web support";

    public string WebEnabled => IsCzech ? "Zapnuto v Apache" : "Enabled in Apache";

    public string WebDisabled => IsCzech ? "Vypnuto v Apache" : "Disabled in Apache";

    public string WebNotConfigured => IsCzech ? "Nenastaveno" : "Not configured";

    public string NotServedByApache => IsCzech ? "Projekt není poskytován přes Apache" : "Project is not served by Apache";

    public string WebNotConfiguredDetail => IsCzech
        ? "Projekt zatím nemá webový kořen."
        : "The project does not have a web root yet.";

    public string WebRootSummary(string root) => IsCzech ? $"Web root: {root}" : $"Web root: {root}";

    public string ConfigureWebProject => IsCzech ? "Nastavit web" : "Configure web";

    public string SaveWebConfiguration => IsCzech ? "Uložit nastavení" : "Save settings";

    public string ServeProjectThroughApache => IsCzech
        ? "Poskytovat projekt přes Apache"
        : "Serve this project through Apache";

    public string AllowHtaccessLabel => IsCzech
        ? "Povolit projektové soubory .htaccess"
        : "Allow project .htaccess files";

    public string HtaccessStatus(bool allowed) => allowed
        ? IsCzech ? ".htaccess je povolen" : ".htaccess is allowed"
        : IsCzech ? ".htaccess je vypnutý" : ".htaccess is disabled";

    public string WebSettingsHelp => IsCzech
        ? "Nastavení nemění zdrojové soubory projektu. Chybějící web root se vytvoří uvnitř projektu; běžící Apache se restartuje až po samostatném potvrzení."
        : "These settings do not rewrite project source files. A missing web root is created inside the project; a running Apache instance restarts only after separate confirmation.";

    public string DefaultProjectWebRequired => IsCzech
        ? "Výchozí localhost musí zůstat v Apache zapnutý."
        : "The default localhost site must remain enabled in Apache.";

    public string ConfigureWebRootPrompt => IsCzech
        ? "Web root uvnitř projektu (například public nebo .):"
        : "Web root inside the project (for example public or .):";

    public string ConfigureWebRootValidation => IsCzech
        ? "Zadejte bezpečnou relativní složku uvnitř projektu."
        : "Enter a safe relative directory inside the project.";

    public string OpenWebUrl => "URL";

    public string ApplyWebConfiguration => IsCzech
        ? "Použít změny a restartovat Apache"
        : "Apply changes and restart Apache";

    public string WebConfigurationRestartPending => IsCzech
        ? "Webové nastavení je uložené. Běžící Apache zatím používá předchozí konfiguraci."
        : "Web settings are saved. The running Apache instance is still using the previous configuration.";

    public string WebConfigurationSavedForNextStart => IsCzech
        ? "Webové nastavení je uložené a použije se při příštím spuštění Apache."
        : "Web settings are saved and will be used the next time Apache starts.";

    public string WebConfigurationApplied => IsCzech
        ? "Webové nastavení bylo použito a Apache restartován."
        : "Web settings were applied and Apache was restarted.";

    public string ProjectDirectoryMissing => IsCzech ? "Složka projektu chybí" : "Project directory is missing";

    public string RenameProject => IsCzech ? "Přejmenovat" : "Rename";

    public string RenameProjectPrompt => IsCzech ? "Nový zobrazovaný název projektu:" : "New project display name:";

    public string RenameProjectValidation => IsCzech ? "Zadejte neprázdný název projektu." : "Enter a non-empty project name.";

    public string ProjectRenamed(string name) => IsCzech ? $"Projekt byl přejmenován na {name}." : $"Project renamed to {name}.";

    public string OpenFiles => IsCzech ? "Soubory" : "Files";

    public string OpenTerminal => IsCzech ? "Terminál" : "Terminal";

    public string UnregisterProject => IsCzech ? "Odebrat ze seznamu" : "Unregister";

    public string UnregisterProjectQuestion(string name) => IsCzech
        ? $"Odebrat projekt {name} ze seznamu? Jeho soubory se nesmažou a naplánované úlohy se vypnou."
        : $"Unregister project {name}? Its files will not be deleted and its scheduled tasks will be disabled.";

    public string ProjectUnregistered(string name) => IsCzech
        ? $"Projekt {name} byl odebrán ze seznamu. Všechny soubory zůstaly zachované."
        : $"Project {name} was unregistered. All files were preserved.";

    public string ProjectDirectoryUnavailable => IsCzech
        ? "Projekt nelze aktivovat, protože jeho složka na disku chybí."
        : "The project cannot be activated because its directory is missing.";

    public string CreateGeneralProject => IsCzech ? "Vytvořit nový projekt" : "Create a new project";

    public string ProjectTemplate => IsCzech ? "Počáteční šablona" : "Initial template";

    public string ProjectTemplateNotice => IsCzech
        ? "Šablona vytvoří jen počáteční soubory. Neurčuje typ projektu a nic nestahuje ani nespouští."
        : "A template only creates initial files. It does not define a project type and downloads or runs nothing.";

    public string ProjectTemplateName(ProjectTemplateKind kind) => kind switch
    {
        ProjectTemplateKind.Empty => IsCzech ? "Prázdný" : "Empty",
        ProjectTemplateKind.Web => "Web",
        ProjectTemplateKind.Python => "Python",
        ProjectTemplateKind.BrowserAutomation => IsCzech ? "Automatizace prohlížeče" : "Browser automation",
        ProjectTemplateKind.NodeJs => "Node.js",
        _ => kind.ToString()
    };

    public string ProjectTemplateDescription(ProjectTemplateKind kind) => kind switch
    {
        ProjectTemplateKind.Empty => IsCzech ? "Pouze prázdná složka projektu." : "Only an empty project directory.",
        ProjectTemplateKind.Web => IsCzech ? "Statická úvodní stránka v public/ a zapnutý Apache web prostor." : "A static starter page in public/ and enabled Apache web space.",
        ProjectTemplateKind.Python => IsCzech ? "main.py a prázdný requirements.txt." : "main.py and an empty requirements.txt.",
        ProjectTemplateKind.BrowserAutomation => IsCzech ? "Malý Selenium příklad a návod ke sdílenému serveru." : "A small Selenium example and shared-server guide.",
        ProjectTemplateKind.NodeJs => IsCzech ? "Minimální package.json a src/index.js." : "A minimal package.json and src/index.js.",
        _ => string.Empty
    };

    public string ProjectCreatedWithoutDownloads(string name) => IsCzech
        ? $"Projekt {name} byl vytvořen a aktivován. Nebyl stažen ani spuštěn žádný runtime."
        : $"Project {name} was created and activated. No runtime was downloaded or executed.";

    public string AddExistingProject => IsCzech ? "Přidat existující portable složku" : "Add an existing portable directory";

    public string ExistingProjectDirectory => IsCzech ? "Nepřiřazená složka" : "Unregistered directory";

    public string ExistingProjectName => IsCzech ? "Zobrazovaný název" : "Display name";

    public string NoExistingProjectDirectories => IsCzech
        ? "Pod instances/default/projects nejsou žádné bezpečné nepřiřazené složky."
        : "There are no safe unregistered directories under instances/default/projects.";

    public string RegisterProject => IsCzech ? "Přidat do projektů" : "Register project";

    public string ProjectRegistered(string name) => IsCzech
        ? $"Existující složka byla přidána jako projekt {name}; její obsah se nezměnil."
        : $"The existing directory was registered as project {name}; its content was not changed.";

    public string ProjectCapability(ProjectCapabilityKind kind) => kind switch
    {
        ProjectCapabilityKind.Web => "Web",
        ProjectCapabilityKind.Php => "PHP",
        ProjectCapabilityKind.NodeJs => "Node.js",
        ProjectCapabilityKind.Python => "Python",
        ProjectCapabilityKind.BrowserAutomation => IsCzech ? "Automatizace" : "Automation",
        _ => kind.ToString()
    };

    public string NoCapabilitiesDetected => IsCzech ? "Zatím nic nerozpoznáno" : "Nothing detected yet";

    public string CapabilityDetectionHint => IsCzech
        ? "Všechny nástroje lze použít i bez rozpoznané technologie."
        : "All tools remain available even without a detected technology.";

    public string SharedRuntimesReady => IsCzech ? "Potřebné sdílené runtime jsou připravené." : "Required shared runtimes are ready.";

    public string MissingSharedRuntimes(IEnumerable<string> runtimes) => IsCzech
        ? $"Chybí sdílené runtime: {string.Join(", ", runtimes)}. Nainstalujte je ručně v Modulech."
        : $"Missing shared runtimes: {string.Join(", ", runtimes)}. Install them explicitly from Modules.";

}
