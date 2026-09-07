using PortableDeveloper.App.ViewModels;
using PortableDeveloper.Application.ProjectTools;
using PortableDeveloper.Application.Settings;

namespace PortableDeveloper.Tests;

public sealed class PackageManagerPageViewModelTests
{
    [Fact]
    public void Shared_host_exposes_the_localized_text_used_by_its_view()
    {
        var settings = new MemorySettingsStore(
            ApplicationSettings.Default with { Language = ApplicationLanguage.English });
        var text = new UiText(settings);
        var page = new PackageManagerPageViewModel(PackageManagerKind.Node, "project");

        var host = new PackageManagerHostViewModel(text, page);

        Assert.Same(text, host.Text);
        Assert.Equal(text.NodeHelp, host.HelpText);
        Assert.False(string.IsNullOrWhiteSpace(host.Text.InstallPackage));
        Assert.False(string.IsNullOrWhiteSpace(host.Text.RemovePackage));
    }

    [Theory]
    [InlineData(PackageManagerKind.Composer)]
    [InlineData(PackageManagerKind.Node)]
    [InlineData(PackageManagerKind.Python)]
    public void Manager_identity_is_explicit_and_stable(PackageManagerKind kind)
    {
        var page = new PackageManagerPageViewModel(kind, "instances/default/projects/example");

        Assert.Equal(kind, page.Kind);
        Assert.Equal("instances/default/projects/example", page.ProjectRelativePath);
    }

    [Fact]
    public void Package_inventory_separates_direct_and_transitive_dependencies()
    {
        var page = new PackageManagerPageViewModel(PackageManagerKind.Composer, "project");
        var direct = new ProjectPackageInfo(
            "vendor/a-very-long-direct-package-name",
            "1.2.3",
            "A long description that the shared view is expected to wrap.",
            IsDirectDependency: true);
        var transitive = new ProjectPackageInfo("vendor/transitive", "4.5.6");

        page.SetPackages([direct, transitive]);

        Assert.Equal([direct, transitive], page.Packages);
        Assert.Equal([direct], page.DirectPackages);
        Assert.Equal([transitive], page.TransitivePackages);
        Assert.False(page.NoPackages);
        Assert.True(page.HasTransitivePackages);
    }

    [Fact]
    public void Operation_result_remains_local_until_the_page_clears_it()
    {
        var page = new PackageManagerPageViewModel(PackageManagerKind.Python, "project");
        var progress = new ProjectPackageOperationProgress(
            ProjectPackageOperationKind.Install,
            ProjectPackageOperationPhase.RunningPackageManager,
            "selenium",
            IsIndeterminate: false,
            Percentage: 45);

        page.SetOperationProgress(progress, "Installing", "selenium");

        Assert.True(page.OperationVisible);
        Assert.False(page.OperationIndeterminate);
        Assert.Equal(45, page.OperationPercentage);
        Assert.Equal("Installing", page.OperationStatus);
        Assert.Equal("selenium", page.OperationDetail);

        page.SetOperationResult("Installed", isSuccess: true);
        Assert.Equal(100, page.OperationPercentage);

        page.ClearOperation();
        Assert.False(page.OperationVisible);
        Assert.Equal(string.Empty, page.OperationStatus);
        Assert.Equal(string.Empty, page.OperationDetail);
    }

    [Fact]
    public void Project_change_clears_inventory_and_operation_from_the_previous_project()
    {
        var page = new PackageManagerPageViewModel(PackageManagerKind.Composer, "projects/first");
        page.SetPackages([new ProjectPackageInfo("vendor/package", "1.0.0", IsDirectDependency: true)]);
        page.SetOperationResult("Installed", isSuccess: true, "vendor/package");
        page.SetStatus("Old inventory");

        page.SetProjectRelativePath("projects/second");

        Assert.Equal("projects/second", page.ProjectRelativePath);
        Assert.Empty(page.Packages);
        Assert.Empty(page.DirectPackages);
        Assert.Empty(page.TransitivePackages);
        Assert.True(page.NoPackages);
        Assert.False(page.OperationVisible);
        Assert.Equal(string.Empty, page.Status);
    }

    private sealed class MemorySettingsStore(ApplicationSettings settings) : IApplicationSettingsStore
    {
        private ApplicationSettings _settings = settings;

        public ApplicationSettings Load() => _settings;

        public void Save(ApplicationSettings settings) => _settings = settings;
    }
}
