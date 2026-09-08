using System.Collections.ObjectModel;
using PortableDeveloper.Application.ApachePhp;
using PortableDeveloper.Application.MariaDb;
using PortableDeveloper.Application.Modules;
using PortableDeveloper.Application.Packages;
using PortableDeveloper.Application.Ports;
using PortableDeveloper.Application.Selenium;
using PortableDeveloper.Application.Services;
using PortableDeveloper.Domain.Modules;
using PortableDeveloper.Domain.Processes;

namespace PortableDeveloper.App.ViewModels;

public sealed class WorkspaceRuntimeCoordinator
{
    private readonly IModuleInventory _moduleInventory;
    private readonly IModuleInstallationVerifier _moduleVerifier;
    private readonly IApacheRuntimePreflight _apacheRuntimePreflight;
    private readonly IRuntimePackageManager _runtimePackageManager;
    private readonly List<ServiceCardViewModel> _serviceCards = [];
    private ManagedProcessState _apacheProcessState = ManagedProcessState.Stopped;
    private string _apacheErrorDetail = string.Empty;
    private bool _webConfigurationRestartRequired;
    private MariaDbInstanceState _mariaDbState;
    private ManagedProcessState _mariaDbProcessState = ManagedProcessState.Stopped;
    private string _mariaDbErrorDetail = string.Empty;
    private bool _mariaDbOperationInProgress;
    private bool _rootPasswordSet;
    private ManagedProcessState _seleniumProcessState = ManagedProcessState.Stopped;
    private string _seleniumErrorDetail = string.Empty;
    private bool _seleniumOperationInProgress;
    private SeleniumServerOptions _seleniumOptions = SeleniumServerOptions.Default;
    private int _seleniumReadyEnvironmentCount;
    private PortSettings _portSettings;

    public WorkspaceRuntimeCoordinator(
        UiText text,
        IModuleInventory moduleInventory,
        IModuleInstallationVerifier moduleVerifier,
        IApacheRuntimePreflight apacheRuntimePreflight,
        IRuntimePackageManager runtimePackageManager,
        MariaDbInstanceState mariaDbState,
        PortSettings portSettings)
    {
        Text = text;
        _moduleInventory = moduleInventory;
        _moduleVerifier = moduleVerifier;
        _apacheRuntimePreflight = apacheRuntimePreflight;
        _runtimePackageManager = runtimePackageManager;
        _mariaDbState = mariaDbState;
        _portSettings = PortSettingsValidator.Validate(portSettings);
        RuntimePackages = [];
        SeleniumDriverPackages = [];
        RefreshRuntimePackages();
        RefreshServiceCards();
    }

    public event EventHandler? StateChanged;

    public event EventHandler? AvailabilityChanged;

    public UiText Text { get; }

    public ObservableCollection<RuntimePackageViewModel> RuntimePackages { get; }

    public ObservableCollection<RuntimePackageViewModel> SeleniumDriverPackages { get; }

    public ServiceCardViewModel ApacheService => GetServiceCard("Apache");

    public ServiceCardViewModel MariaDbService => GetServiceCard("MariaDB");

    public ServiceCardViewModel SeleniumService => GetServiceCard("Selenium");

    public bool ApacheReady => IsVerified(ModuleKind.Apache, "Apache");

    public bool PhpReady => IsVerified(ModuleKind.Php, "PHP");

    public string PhpRuntimeVersion => _moduleInventory.GetInstalled(ModuleKind.Php).FirstOrDefault()?.Version ?? string.Empty;

    public bool MariaDbInstalled => IsVerified(ModuleKind.MariaDb, "MariaDB");

    public bool SeleniumInstalled => IsRuntimePackageInstalled(RuntimePackageKind.Selenium);

    public bool PhpMyAdminInstalled => IsRuntimePackageInstalled(RuntimePackageKind.PhpMyAdmin);

    public PortSettings PortSettings => _portSettings;

    public int ApachePort => _portSettings.ApachePort;

    public int PhpFastCgiPort => _portSettings.PhpFastCgiPort;

    public int MariaDbPort => _portSettings.MariaDbPort;

    public int SeleniumPort => _portSettings.SeleniumPort;

    public bool PortSettingsEnabled =>
        !_mariaDbOperationInProgress
        && !_seleniumOperationInProgress
        && _apacheProcessState is not ManagedProcessState.Running and not ManagedProcessState.Starting and not ManagedProcessState.Stopping
        && _mariaDbProcessState is not ManagedProcessState.Running and not ManagedProcessState.Starting and not ManagedProcessState.Stopping
        && _seleniumProcessState is not ManagedProcessState.Running and not ManagedProcessState.Starting and not ManagedProcessState.Stopping;

    public string PortSettingsAvailability => PortSettingsEnabled
        ? Text.PortSettingsReady
        : Text.PortSettingsRequireStoppedServices;

    public int SeleniumMaxSessions => _seleniumOptions.MaxSessions;

    public int SeleniumSessionTimeoutSeconds => _seleniumOptions.SessionTimeoutSeconds;

    public string SeleniumHubUrl => $"http://127.0.0.1:{SeleniumPort}/";

    public ManagedProcessState SeleniumProcessState => _seleniumProcessState;

    public bool SeleniumIsRunning => _seleniumProcessState == ManagedProcessState.Running;

    public bool SeleniumActionEnabled => !_seleniumOperationInProgress
        && _seleniumProcessState is not ManagedProcessState.Starting and not ManagedProcessState.Stopping
        && (SeleniumIsRunning || _seleniumReadyEnvironmentCount > 0);

    public bool SeleniumSettingsEnabled => !_seleniumOperationInProgress
        && _seleniumProcessState is not ManagedProcessState.Starting and not ManagedProcessState.Stopping;

    public string SeleniumSettingsActionLabel => SeleniumIsRunning
        ? Text.SaveAndRestartSelenium
        : Text.SaveSeleniumSettings;

    public bool SeleniumProfileActionsEnabled => !_seleniumOperationInProgress;

    public bool SeleniumSessionActionsEnabled => SeleniumIsRunning && !_seleniumOperationInProgress;

    public string SeleniumActionLabel => Text.SeleniumAction(_seleniumProcessState);

    public ManagedProcessState MariaDbProcessState => _mariaDbProcessState;

    public bool MariaDbIsRunning => _mariaDbProcessState == ManagedProcessState.Running;

    public bool DatabaseActionsEnabled => MariaDbIsRunning && !_mariaDbOperationInProgress;

    public bool MariaDbActionEnabled => _mariaDbState == MariaDbInstanceState.Initialized
        && !_mariaDbOperationInProgress
        && _mariaDbProcessState is not ManagedProcessState.Starting and not ManagedProcessState.Stopping;

    public string MariaDbActionLabel => Text.MariaDbAction(_mariaDbProcessState);

    public bool RootPasswordSet => _rootPasswordSet;

    public string RootPasswordState => _rootPasswordSet ? Text.PasswordConfigured : Text.NoPasswordConfigured;

    public string RootPasswordActionLabel => _rootPasswordSet ? Text.ChangePassword : Text.SetPassword;

    public string PhpMyAdminUrl => $"http://127.0.0.1:{ApachePort}/phpmyadmin/";

    public PhpMyAdminAvailability PhpMyAdminState =>
        ServiceDependencyPolicy.GetPhpMyAdminAvailability(_apacheProcessState, _mariaDbProcessState);

    public bool PhpMyAdminActionEnabled => PhpMyAdminInstalled
        && PhpReady
        && !_mariaDbOperationInProgress
        && PhpMyAdminState == PhpMyAdminAvailability.Ready;

    public string PhpMyAdminDependencyState => !PhpReady ? Text.PhpMyAdminNeedsPhp : PhpMyAdminState switch
    {
        PhpMyAdminAvailability.Ready => Text.PhpMyAdminReady,
        PhpMyAdminAvailability.NeedsWeb => Text.PhpMyAdminNeedsWeb,
        PhpMyAdminAvailability.NeedsDatabase => Text.PhpMyAdminNeedsDatabase,
        _ => Text.PhpMyAdminNeedsBoth
    };

    public ManagedProcessState ApacheProcessState => _apacheProcessState;

    public bool ApacheIsRunning => _apacheProcessState == ManagedProcessState.Running;

    public string ApacheActionLabel => Text.ApacheAction(_apacheProcessState);

    public bool ApacheActionEnabled => ApacheReady
        && _apacheProcessState is not ManagedProcessState.Starting and not ManagedProcessState.Stopping;

    public bool ApacheRestartEnabled => ApacheIsRunning && ApacheActionEnabled;

    public bool WebConfigurationRestartRequired => _webConfigurationRestartRequired;

    public bool WebConfigurationRestartPromptVisible => WebConfigurationRestartRequired && ApacheIsRunning;

    public bool WebConfigurationApplyEnabled => WebConfigurationRestartRequired && ApacheRestartEnabled;

    public string PhpSettingsActionLabel => ApacheIsRunning ? Text.SaveAndRestartPhp : Text.SavePhpSettings;

    public bool PhpSettingsEnabled => _apacheProcessState is not ManagedProcessState.Starting and not ManagedProcessState.Stopping;

    public bool IsPageAvailable(NavigationPage page) => page switch
    {
        NavigationPage.Apache => ApacheReady,
        NavigationPage.Php => PhpReady,
        NavigationPage.Databases => MariaDbInstalled,
        NavigationPage.Selenium => SeleniumInstalled,
        NavigationPage.Composer => IsRuntimePackageInstalled(RuntimePackageKind.Composer),
        NavigationPage.Node => IsRuntimePackageInstalled(RuntimePackageKind.Node),
        NavigationPage.Python => IsRuntimePackageInstalled(RuntimePackageKind.Python),
        _ => true
    };

    public bool IsRuntimePackageInstalled(RuntimePackageKind kind) =>
        RuntimePackages.FirstOrDefault(item => item.Kind == kind)?.IsInstalled == true;

    public void SetWebConfigurationRestartRequired(bool required)
    {
        if (_webConfigurationRestartRequired == required)
        {
            return;
        }

        _webConfigurationRestartRequired = required;
        PublishState();
    }

    public void SetApacheStatus(ManagedProcessState state, string detail)
    {
        _apacheProcessState = state;
        _apacheErrorDetail = state == ManagedProcessState.Failed ? detail : string.Empty;
        RefreshServiceCards();
        PublishState();
    }

    public void SetMariaDbState(MariaDbInstanceState state)
    {
        _mariaDbState = state;
        RefreshServiceCards();
        PublishState();
    }

    public void SetMariaDbOperationInProgress(bool inProgress)
    {
        _mariaDbOperationInProgress = inProgress;
        RefreshServiceCards();
        PublishState();
    }

    public void SetMariaDbStatus(ManagedProcessState state, string detail)
    {
        _mariaDbProcessState = state;
        _mariaDbErrorDetail = state == ManagedProcessState.Failed ? detail : string.Empty;
        RefreshServiceCards();
        PublishState();
    }

    public void SetRootPasswordState(bool isSet)
    {
        _rootPasswordSet = isSet;
        PublishState();
    }

    public void RefreshRuntimeAvailability()
    {
        RefreshRuntimePackages();
        RefreshServiceCards();
        PublishState();
        AvailabilityChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RefreshLocalizedPresentation()
    {
        RefreshRuntimePackages();
        RefreshServiceCards();
        PublishState();
    }

    public void SetSeleniumOptions(SeleniumServerOptions options)
    {
        _seleniumOptions = options;
        PublishState();
    }

    public void SetPortSettings(PortSettings settings)
    {
        _portSettings = PortSettingsValidator.Validate(settings);
        RefreshServiceCards();
        PublishState();
    }

    public void SetSeleniumOperationInProgress(bool inProgress)
    {
        _seleniumOperationInProgress = inProgress;
        RefreshServiceCards();
        PublishState();
    }

    public void SetSeleniumStatus(ManagedProcessState state, string detail)
    {
        _seleniumProcessState = state;
        _seleniumErrorDetail = state == ManagedProcessState.Failed ? detail : string.Empty;
        RefreshServiceCards();
        PublishState();
    }

    public void SetSeleniumEnvironmentAvailability(int readyEnvironmentCount)
    {
        _seleniumReadyEnvironmentCount = Math.Max(0, readyEnvironmentCount);
        RefreshServiceCards();
        PublishState();
    }

    private void RefreshRuntimePackages()
    {
        RuntimePackages.Clear();
        SeleniumDriverPackages.Clear();
        foreach (var package in _runtimePackageManager.GetPackages())
        {
            var viewModel = new RuntimePackageViewModel(
                package.Kind,
                Text.RuntimePackageName(package.Kind),
                Text.RuntimePackageDescription(package.Kind),
                package.Version,
                package.IsInstalled,
                string.Empty);
            if (IsSeleniumDriverPackage(package.Kind))
            {
                SeleniumDriverPackages.Add(viewModel);
            }
            else
            {
                RuntimePackages.Add(viewModel);
            }
        }
    }

    private void RefreshServiceCards()
    {
        _serviceCards.Clear();
        AddModuleCard(ModuleKind.Apache, "Apache", "apache");
        AddModuleCard(ModuleKind.MariaDb, "MariaDB", "mariadb");
        AddModuleCard(ModuleKind.Selenium, "Selenium", "selenium");
    }

    private static bool IsSeleniumDriverPackage(RuntimePackageKind kind) => kind is
        RuntimePackageKind.SeleniumChromeEnvironment or
        RuntimePackageKind.SeleniumFirefoxEnvironment;

    private bool IsVerified(ModuleKind kind, string displayName) => _moduleVerifier.Verify(kind, displayName).IsVerified;

    private ServiceCardViewModel GetServiceCard(string name) =>
        _serviceCards.FirstOrDefault(card => string.Equals(card.Name, name, StringComparison.OrdinalIgnoreCase))
        ?? new ServiceCardViewModel(name, string.Empty, Text.ModuleNotFound, Text.NotInstalled);

    private void AddModuleCard(ModuleKind kind, string name, string descriptionKey)
    {
        var description = Text.ServiceDescription(descriptionKey);
        var installation = _moduleInventory.GetInstalled(kind).FirstOrDefault();
        if (installation is null)
        {
            return;
        }

        if (kind == ModuleKind.Apache)
        {
            var readiness = _apacheRuntimePreflight.Check(installation.ModuleRootRelativePath);
            if (!readiness.IsReady)
            {
                _serviceCards.Add(new ServiceCardViewModel(name, description, Text.RuntimeMissing(readiness.MissingFiles), Text.WaitingRuntime));
                return;
            }
        }

        var verification = _moduleVerifier.Verify(kind, name);
        if (!verification.IsVerified)
        {
            _serviceCards.Add(new ServiceCardViewModel(name, description, verification.Detail, Text.VerificationFailed));
            return;
        }

        _serviceCards.Add(kind switch
        {
            ModuleKind.Apache => CreateApacheServiceCard(name, description, installation.Version),
            ModuleKind.MariaDb => CreateMariaDbCard(name, description, installation.Version),
            ModuleKind.Selenium => CreateSeleniumCard(name, description, installation.Version),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        });
    }

    private ServiceCardViewModel CreateApacheServiceCard(string name, string description, string version)
    {
        var state = _apacheProcessState switch
        {
            ManagedProcessState.Running => Text.Running,
            ManagedProcessState.Starting => Text.Starting,
            ManagedProcessState.Stopping => Text.Stopping,
            ManagedProcessState.Failed => Text.Failed,
            _ => Text.Stopped
        };
        var detail = _apacheProcessState == ManagedProcessState.Running
            ? Text.ApacheRuntimeDetail(version, ApachePort, PhpReady)
            : Text.ApacheReadyDetail(version, PhpReady);
        return new ServiceCardViewModel(
            name,
            description,
            detail,
            state,
            "toggle-apache",
            Text.ApacheAction(_apacheProcessState),
            ApacheActionEnabled,
            version);
    }

    private ServiceCardViewModel CreateMariaDbCard(string name, string description, string version) => _mariaDbState switch
    {
        MariaDbInstanceState.Initialized => new(
            name,
            description,
            _mariaDbProcessState == ManagedProcessState.Failed
                ? _mariaDbErrorDetail
                : Text.MariaDbRuntimeDetail(version, _mariaDbProcessState, MariaDbPort),
            Text.StackStatus(_mariaDbProcessState),
            "toggle-mariadb",
            Text.MariaDbAction(_mariaDbProcessState),
            MariaDbActionEnabled,
            version),
        MariaDbInstanceState.Incomplete => new(
            name,
            description,
            Text.MariaDbInstanceIncomplete,
            Text.NeedsAttention,
            Version: version),
        _ => new(
            name,
            description,
            Text.MariaDbNeedsPreparation(version),
            _mariaDbOperationInProgress ? Text.Starting : Text.NeedsSetup,
            Version: version)
    };

    private ServiceCardViewModel CreateSeleniumCard(string name, string description, string version) => new(
        name,
        description,
        _seleniumProcessState == ManagedProcessState.Failed
            ? _seleniumErrorDetail
            : Text.SeleniumRuntimeDetail(version, _seleniumProcessState, SeleniumPort, _seleniumReadyEnvironmentCount),
        Text.StackStatus(_seleniumProcessState),
        "toggle-selenium",
        Text.SeleniumAction(_seleniumProcessState),
        SeleniumActionEnabled,
        version);

    private void PublishState() => StateChanged?.Invoke(this, EventArgs.Empty);
}
