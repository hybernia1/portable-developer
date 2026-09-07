using PortableDeveloper.App.ViewModels;
using PortableDeveloper.Application.ApachePhp;
using PortableDeveloper.Application.MariaDb;
using PortableDeveloper.Application.Modules;
using PortableDeveloper.Application.Packages;
using PortableDeveloper.Application.Ports;
using PortableDeveloper.Application.Settings;
using PortableDeveloper.Domain.Modules;
using PortableDeveloper.Domain.Processes;

namespace PortableDeveloper.Tests;

public sealed class WorkspaceRuntimeCoordinatorTests
{
    [Fact]
    public void Runtime_state_updates_one_cohesive_snapshot_without_owning_feature_pages()
    {
        var apache = new ModuleInstallation(ModuleKind.Apache, "2.4", "modules/apache", "bin/httpd.exe");
        var mariaDb = new ModuleInstallation(ModuleKind.MariaDb, "11.8", "modules/mariadb", "bin/mariadbd.exe");
        var inventory = new TestModuleInventory([apache, mariaDb]);
        var ports = new PortSettings(8080, 9070, 3307, 4445);
        var text = new UiText(new InMemorySettingsStore());
        var runtime = new WorkspaceRuntimeCoordinator(
            text,
            inventory,
            new InventoryModuleVerifier(inventory),
            new ReadyApachePreflight(),
            new TestRuntimePackageManager([
                new RuntimePackageInfo(RuntimePackageKind.Selenium, "4.35", true, string.Empty, []),
                new RuntimePackageInfo(RuntimePackageKind.Node, "24.7", true, string.Empty, [])
            ]),
            MariaDbInstanceState.Initialized,
            ports);
        var changes = 0;
        runtime.StateChanged += (_, _) => changes++;

        runtime.SetApacheStatus(ManagedProcessState.Running, string.Empty);
        runtime.SetMariaDbStatus(ManagedProcessState.Running, string.Empty);
        runtime.SetSeleniumEnvironmentAvailability(1);

        Assert.Equal(3, changes);
        Assert.True(runtime.ApacheIsRunning);
        Assert.True(runtime.MariaDbIsRunning);
        Assert.True(runtime.SeleniumInstalled);
        Assert.True(runtime.IsPageAvailable(NavigationPage.Node));
        Assert.False(runtime.PortSettingsEnabled);
        Assert.Equal(ports, runtime.PortSettings);
        Assert.Equal(text.Running, runtime.ApacheService.State);
        Assert.Equal(text.StackStatus(ManagedProcessState.Running), runtime.MariaDbService.State);
    }

    private sealed class TestModuleInventory(IReadOnlyList<ModuleInstallation> installations) : IModuleInventory
    {
        public IReadOnlyList<ModuleInstallation> GetInstalled(ModuleKind kind) =>
            installations.Where(item => item.Kind == kind).ToArray();
    }

    private sealed class InventoryModuleVerifier(IModuleInventory inventory) : IModuleInstallationVerifier
    {
        public ModuleInstallationVerification Verify(ModuleKind kind, string displayName) =>
            new(inventory.GetInstalled(kind).FirstOrDefault(), displayName);
    }

    private sealed class ReadyApachePreflight : IApacheRuntimePreflight
    {
        public ApacheRuntimeReadiness Check(string apacheModuleRootRelativePath) => new(true, []);
    }

    private sealed class TestRuntimePackageManager(IReadOnlyList<RuntimePackageInfo> packages) : IRuntimePackageManager
    {
        public IReadOnlyList<RuntimePackageInfo> GetPackages() => packages;

        public Task<RuntimePackageInstallResult> InstallAsync(
            RuntimePackageKind package,
            IProgress<RuntimePackageInstallProgress>? progress = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class InMemorySettingsStore : IApplicationSettingsStore
    {
        private ApplicationSettings _settings = ApplicationSettings.Default;

        public ApplicationSettings Load() => _settings;

        public void Save(ApplicationSettings settings) => _settings = settings;
    }
}
