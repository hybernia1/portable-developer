using System.Windows;
using System.Windows.Controls;
using PortableDeveloper.App.ViewModels;

namespace PortableDeveloper.App.Controls;

public partial class PackageManagerView : UserControl
{
    public static readonly RoutedEvent OpenProjectRequestedEvent = RegisterEvent(nameof(OpenProjectRequested), typeof(EventHandler));
    public static readonly RoutedEvent RefreshRequestedEvent = RegisterEvent(nameof(RefreshRequested), typeof(EventHandler));
    public static readonly RoutedEvent HelpRequestedEvent = RegisterEvent(nameof(HelpRequested), typeof(EventHandler));
    public static readonly RoutedEvent InstallRequestedEvent = RegisterEvent(nameof(InstallRequested), typeof(EventHandler<PackageInstallRequestedEventArgs>));
    public static readonly RoutedEvent RemoveRequestedEvent = RegisterEvent(nameof(RemoveRequested), typeof(EventHandler<PackageRemoveRequestedEventArgs>));

    public static readonly DependencyProperty PageProperty = DependencyProperty.Register(
        nameof(Page),
        typeof(PackageManagerPageViewModel),
        typeof(PackageManagerView));

    public static readonly DependencyProperty HeaderTitleProperty = DependencyProperty.Register(
        nameof(HeaderTitle),
        typeof(string),
        typeof(PackageManagerView),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty BrandLogoProperty = DependencyProperty.Register(
        nameof(BrandLogo),
        typeof(string),
        typeof(PackageManagerView),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty HelpTextProperty = DependencyProperty.Register(
        nameof(HelpText),
        typeof(string),
        typeof(PackageManagerView),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty PackageExampleProperty = DependencyProperty.Register(
        nameof(PackageExample),
        typeof(string),
        typeof(PackageManagerView),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ConstraintExampleProperty = DependencyProperty.Register(
        nameof(ConstraintExample),
        typeof(string),
        typeof(PackageManagerView),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty PackageNameInputProperty = DependencyProperty.Register(
        nameof(PackageNameInput),
        typeof(string),
        typeof(PackageManagerView),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty VersionConstraintInputProperty = DependencyProperty.Register(
        nameof(VersionConstraintInput),
        typeof(string),
        typeof(PackageManagerView),
        new PropertyMetadata(string.Empty));

    public PackageManagerView()
    {
        InitializeComponent();
    }

    public event EventHandler OpenProjectRequested
    {
        add => AddHandler(OpenProjectRequestedEvent, value);
        remove => RemoveHandler(OpenProjectRequestedEvent, value);
    }

    public event EventHandler RefreshRequested
    {
        add => AddHandler(RefreshRequestedEvent, value);
        remove => RemoveHandler(RefreshRequestedEvent, value);
    }

    public event EventHandler HelpRequested
    {
        add => AddHandler(HelpRequestedEvent, value);
        remove => RemoveHandler(HelpRequestedEvent, value);
    }

    public event EventHandler<PackageInstallRequestedEventArgs> InstallRequested
    {
        add => AddHandler(InstallRequestedEvent, value);
        remove => RemoveHandler(InstallRequestedEvent, value);
    }

    public event EventHandler<PackageRemoveRequestedEventArgs> RemoveRequested
    {
        add => AddHandler(RemoveRequestedEvent, value);
        remove => RemoveHandler(RemoveRequestedEvent, value);
    }

    public PackageManagerPageViewModel? Page
    {
        get => (PackageManagerPageViewModel?)GetValue(PageProperty);
        set => SetValue(PageProperty, value);
    }

    public string HeaderTitle
    {
        get => (string)GetValue(HeaderTitleProperty);
        set => SetValue(HeaderTitleProperty, value);
    }

    public string BrandLogo
    {
        get => (string)GetValue(BrandLogoProperty);
        set => SetValue(BrandLogoProperty, value);
    }

    public string HelpText
    {
        get => (string)GetValue(HelpTextProperty);
        set => SetValue(HelpTextProperty, value);
    }

    public string PackageExample
    {
        get => (string)GetValue(PackageExampleProperty);
        set => SetValue(PackageExampleProperty, value);
    }

    public string ConstraintExample
    {
        get => (string)GetValue(ConstraintExampleProperty);
        set => SetValue(ConstraintExampleProperty, value);
    }

    public string PackageNameInput
    {
        get => (string)GetValue(PackageNameInputProperty);
        set => SetValue(PackageNameInputProperty, value);
    }

    public string VersionConstraintInput
    {
        get => (string)GetValue(VersionConstraintInputProperty);
        set => SetValue(VersionConstraintInputProperty, value);
    }

    public void ClearPackageInput()
    {
        PackageNameInput = string.Empty;
        VersionConstraintInput = string.Empty;
    }

    private static RoutedEvent RegisterEvent(string name, Type handlerType) => EventManager.RegisterRoutedEvent(
        name,
        RoutingStrategy.Bubble,
        handlerType,
        typeof(PackageManagerView));

    private void OpenProject_Click(object sender, RoutedEventArgs e) =>
        RaiseEvent(new RoutedEventArgs(OpenProjectRequestedEvent));

    private void RefreshPackages_Click(object sender, RoutedEventArgs e) =>
        RaiseEvent(new RoutedEventArgs(RefreshRequestedEvent));

    private void ShowHelp_Click(object sender, RoutedEventArgs e) =>
        RaiseEvent(new RoutedEventArgs(HelpRequestedEvent));

    private void InstallPackage_Click(object sender, RoutedEventArgs e) =>
        RaiseEvent(new PackageInstallRequestedEventArgs(
            InstallRequestedEvent,
            PackageNameInput.Trim(),
            VersionConstraintInput.Trim()));

    private void RemovePackage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string packageName })
        {
            RaiseEvent(new PackageRemoveRequestedEventArgs(RemoveRequestedEvent, packageName));
        }
    }
}

public sealed class PackageInstallRequestedEventArgs(
    RoutedEvent routedEvent,
    string packageName,
    string versionConstraint) : RoutedEventArgs(routedEvent)
{
    public string PackageName { get; } = packageName;

    public string VersionConstraint { get; } = versionConstraint;
}

public sealed class PackageRemoveRequestedEventArgs(RoutedEvent routedEvent, string packageName) : RoutedEventArgs(routedEvent)
{
    public string PackageName { get; } = packageName;
}
