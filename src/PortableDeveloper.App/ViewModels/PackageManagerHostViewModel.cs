using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PortableDeveloper.App.ViewModels;

public sealed class PackageManagerHostViewModel : INotifyPropertyChanged
{
    public PackageManagerHostViewModel(UiText text, PackageManagerPageViewModel page)
    {
        Text = text;
        Page = page;
        Text.PropertyChanged += Text_PropertyChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public UiText Text { get; }

    public PackageManagerPageViewModel Page { get; }

    public string HeaderTitle => Page.Kind switch
    {
        PackageManagerKind.Composer => "Composer",
        PackageManagerKind.Node => "Node.js + npm",
        PackageManagerKind.Python => "Python",
        _ => string.Empty
    };

    public string BrandLogo => Page.Kind switch
    {
        PackageManagerKind.Composer => "composer",
        PackageManagerKind.Node => "nodejs",
        PackageManagerKind.Python => "python",
        _ => string.Empty
    };

    public string HelpText => Page.Kind switch
    {
        PackageManagerKind.Composer => Text.ComposerHelp,
        PackageManagerKind.Node => Text.NodeHelp,
        PackageManagerKind.Python => Text.PythonHelp,
        _ => string.Empty
    };

    public string PackageExample => Page.Kind switch
    {
        PackageManagerKind.Composer => Text.ComposerPackageExample,
        PackageManagerKind.Node => Text.NodePackageExample,
        PackageManagerKind.Python => Text.PythonPackageExample,
        _ => string.Empty
    };

    public string ConstraintExample => Page.Kind switch
    {
        PackageManagerKind.Composer => Text.ComposerConstraintExample,
        PackageManagerKind.Node => Text.NodeConstraintExample,
        PackageManagerKind.Python => Text.PythonConstraintExample,
        _ => string.Empty
    };

    private void Text_PropertyChanged(object? sender, PropertyChangedEventArgs e) => OnPropertyChanged(string.Empty);

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
