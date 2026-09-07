using System.Collections.ObjectModel;
using System.Collections.Specialized;
using PortableDeveloper.Application.Packages;

namespace PortableDeveloper.App.ViewModels;

public sealed class ModulesPageViewModel
{
    public ModulesPageViewModel(
        UiText text,
        ObservableCollection<RuntimePackageViewModel> runtimePackages)
    {
        Text = text;
        RuntimePackages = runtimePackages;
        WebStackPackages = [];
        DevelopmentPackages = [];
        AutomationPackages = [];
        RuntimePackages.CollectionChanged += RuntimePackages_CollectionChanged;
        RefreshGroups();
    }

    public UiText Text { get; }

    public ObservableCollection<RuntimePackageViewModel> RuntimePackages { get; }

    public ObservableCollection<RuntimePackageViewModel> WebStackPackages { get; }

    public ObservableCollection<RuntimePackageViewModel> DevelopmentPackages { get; }

    public ObservableCollection<RuntimePackageViewModel> AutomationPackages { get; }

    private void RuntimePackages_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => RefreshGroups();

    private void RefreshGroups()
    {
        WebStackPackages.Clear();
        DevelopmentPackages.Clear();
        AutomationPackages.Clear();
        foreach (var package in RuntimePackages)
        {
            GetGroup(package.Kind).Add(package);
        }
    }

    private ObservableCollection<RuntimePackageViewModel> GetGroup(RuntimePackageKind kind) => kind switch
    {
        RuntimePackageKind.Apache or
        RuntimePackageKind.Php or
        RuntimePackageKind.Database or
        RuntimePackageKind.PhpMyAdmin => WebStackPackages,
        RuntimePackageKind.Selenium => AutomationPackages,
        _ => DevelopmentPackages
    };
}
