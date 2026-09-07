using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using PortableDeveloper.App.ViewModels;
using PortableDeveloper.Application.Projects;

namespace PortableDeveloper.App.Shell;

public sealed class WorkspaceShellViewModel : INotifyPropertyChanged
{
    private readonly GlobalOperationViewModel _globalOperation;
    private readonly Dictionary<NavigationPage, bool> _serviceStates = [];
    private NavigationPage _selectedPage = NavigationPage.Projects;
    private NavigationSection _selectedSection;
    private string _activeProjectId = ProjectCatalogDefaults.DefaultProjectId;
    private string _projectContextStatus = string.Empty;
    private WorkspaceLayoutMode _layoutMode = WorkspaceLayoutMode.Wide;

    public WorkspaceShellViewModel(
        UiText text,
        string applicationVersion,
        ObservableCollection<ProjectViewModel> projects,
        GlobalOperationViewModel globalOperation)
    {
        Text = text;
        ApplicationVersion = applicationVersion;
        Projects = projects;
        _globalOperation = globalOperation;
        NavigationItems = [];
        NavigationSections = [];
        _globalOperation.PropertyChanged += GlobalOperation_PropertyChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public UiText Text { get; }

    public string ApplicationVersion { get; }

    public ObservableCollection<ProjectViewModel> Projects { get; }

    public ObservableCollection<NavigationItemViewModel> NavigationItems { get; }

    public ObservableCollection<NavigationSectionViewModel> NavigationSections { get; }

    public bool CanNavigate => _globalOperation.IsIdle;

    public bool CanChangeProject => _globalOperation.IsIdle;

    public WorkspaceLayoutMode LayoutMode
    {
        get => _layoutMode;
        private set
        {
            if (_layoutMode == value)
            {
                return;
            }

            _layoutMode = value;
            OnPropertyChanged();
        }
    }

    public NavigationPage SelectedPage
    {
        get => _selectedPage;
        set
        {
            if (_selectedPage == value)
            {
                return;
            }

            _selectedPage = value;
            RefreshNavigationSections(resetSelection: true);
            OnPropertyChanged();
            OnPropertyChanged(nameof(PageTitle));
        }
    }

    public string PageTitle => Text.PageTitle(_selectedPage);

    public NavigationSection SelectedSection
    {
        get => _selectedSection;
        set
        {
            if (_selectedSection == value)
            {
                return;
            }

            _selectedSection = value;
            OnPropertyChanged();
        }
    }

    public bool HasNavigationSections => NavigationSections.Count > 0;

    public string ActiveProjectId => _activeProjectId;

    public string ProjectContextStatus
    {
        get => _projectContextStatus;
        private set
        {
            if (_projectContextStatus == value)
            {
                return;
            }

            _projectContextStatus = value;
            OnPropertyChanged();
        }
    }

    public void SetAvailablePages(IEnumerable<NavigationPage> pages)
    {
        NavigationItems.Clear();
        foreach (var page in pages)
        {
            var (groupOrder, itemOrder) = GetNavigationOrder(page);
            var item = new NavigationItemViewModel(
                page,
                Text.NavigationLabel(page),
                Text.NavigationGroup(groupOrder),
                groupOrder,
                itemOrder);
            ApplyServiceStatus(item);
            NavigationItems.Add(item);
        }

        if (NavigationItems.All(item => item.Page != SelectedPage))
        {
            SelectedPage = NavigationPage.Projects;
        }

        RefreshNavigationSections(resetSelection: false);
        OnPropertyChanged(nameof(PageTitle));
    }

    public void SetServiceStatus(NavigationPage page, bool isRunning)
    {
        _serviceStates[page] = isRunning;
        var item = NavigationItems.FirstOrDefault(candidate => candidate.Page == page);
        if (item is not null)
        {
            ApplyServiceStatus(item);
        }
    }

    public void SetActiveProjectId(string activeProjectId)
    {
        if (!string.Equals(_activeProjectId, activeProjectId, StringComparison.OrdinalIgnoreCase))
        {
            _activeProjectId = activeProjectId;
        }

        // The shared project collection is rebuilt before this call. Re-emit the
        // identity even when it did not change so selectors can resolve it again.
        OnPropertyChanged(nameof(ActiveProjectId));
    }

    public void SetProjectContextStatus(string status) => ProjectContextStatus = status;

    public void SetWorkspaceWidth(double availableWidth) =>
        LayoutMode = WorkspaceLayoutPolicy.GetMode(availableWidth);

    private void ApplyServiceStatus(NavigationItemViewModel item)
    {
        if (_serviceStates.TryGetValue(item.Page, out var isRunning))
        {
            item.SetServiceStatus(isRunning, isRunning ? Text.Running : Text.Stopped);
        }
    }

    private void RefreshNavigationSections(bool resetSelection)
    {
        var previousSection = _selectedSection;
        NavigationSections.Clear();
        foreach (var section in WorkspaceNavigationPolicy.GetSections(_selectedPage))
        {
            NavigationSections.Add(new NavigationSectionViewModel(section, Text.NavigationSectionLabel(section)));
        }

        var nextSection = WorkspaceNavigationPolicy.ResolveSection(
            _selectedPage,
            previousSection,
            preserveValidSelection: !resetSelection);
        if (_selectedSection != nextSection)
        {
            _selectedSection = nextSection;
            OnPropertyChanged(nameof(SelectedSection));
        }

        OnPropertyChanged(nameof(HasNavigationSections));
    }

    private void GlobalOperation_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName)
            || e.PropertyName is nameof(GlobalOperationViewModel.IsBusy) or nameof(GlobalOperationViewModel.IsIdle))
        {
            OnPropertyChanged(nameof(CanNavigate));
            OnPropertyChanged(nameof(CanChangeProject));
        }
    }

    private static (int GroupOrder, int ItemOrder) GetNavigationOrder(NavigationPage page) => page switch
    {
        NavigationPage.Projects => (0, 0),
        NavigationPage.Modules => (0, 1),
        NavigationPage.Ports => (0, 2),
        NavigationPage.Apache => (1, 0),
        NavigationPage.Databases => (1, 1),
        NavigationPage.Selenium => (1, 2),
        NavigationPage.Php => (2, 0),
        NavigationPage.Composer => (2, 1),
        NavigationPage.Node => (2, 2),
        NavigationPage.Python => (2, 3),
        NavigationPage.Scheduler => (2, 4),
        NavigationPage.Terminal => (2, 5),
        NavigationPage.Files => (2, 6),
        NavigationPage.Guides => (3, 0),
        NavigationPage.Settings => (3, 1),
        _ => (4, int.MaxValue)
    };

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
