using System.Windows;
using PortableDeveloper.App.Controls;
using PortableDeveloper.App.Views;

namespace PortableDeveloper.App;

public partial class MainWindow
{
    private void RegisterPageActionHandlers()
    {
        AddHandler(ModulesPageView.InstallRequestedEvent, new EventHandler<RuntimePackageInstallRequestedEventArgs>(ModulesPage_InstallRequested));
        AddHandler(ApachePageView.ToggleRequestedEvent, new RoutedEventHandler(ToggleApache_Click));
        AddHandler(PortsPageView.RefreshRequestedEvent, new RoutedEventHandler(RefreshPorts_Click));
        AddHandler(PortsPageView.SaveRequestedEvent, new RoutedEventHandler(SavePorts_Click));
        AddHandler(PortsPageView.InputsChangedEvent, new RoutedEventHandler(PortTextBox_TextChanged));
        AddHandler(SettingsPageView.EditorPreferenceChangedEvent, new EventHandler<EditorPreferenceChangedEventArgs>(EditorPreferenceSelector_SelectionChanged));
        AddHandler(SettingsPageView.RefreshStorageRequestedEvent, new RoutedEventHandler(RefreshStorageUsage_Click));
        AddHandler(SettingsPageView.ClearCacheRequestedEvent, new EventHandler<StorageCacheRequestedEventArgs>(ClearStorageCache_Click));
        AddHandler(SettingsPageView.ClearAllCachesRequestedEvent, new RoutedEventHandler(ClearAllStorageCaches_Click));
        AddHandler(PhpPageView.SaveRequestedEvent, new RoutedEventHandler(SavePhpSettings_Click));
        AddHandler(PhpPageView.ResetRequestedEvent, new RoutedEventHandler(ResetPhpSettings_Click));
        AddHandler(PhpPageView.EditCustomIniRequestedEvent, new RoutedEventHandler(EditCustomPhpIni_Click));
        AddHandler(DatabasesPageView.ToggleRequestedEvent, new RoutedEventHandler(DatabasesPage_ToggleRequested));
        AddHandler(DatabasesPageView.CreateDatabaseRequestedEvent, new RoutedEventHandler(CreateDatabase_Click));
        AddHandler(DatabasesPageView.RefreshRequestedEvent, new RoutedEventHandler(RefreshDatabases_Click));
        AddHandler(DatabasesPageView.ChangePasswordRequestedEvent, new EventHandler<PasswordChangeRequestedEventArgs>(ChangeRootPassword_Click));
        AddHandler(DatabasesPageView.OpenPhpMyAdminRequestedEvent, new RoutedEventHandler(OpenPhpMyAdmin_Click));
        AddHandler(DatabasesPageView.DatabaseActionRequestedEvent, new EventHandler<DatabaseActionRequestedEventArgs>(DatabasesPage_DatabaseActionRequested));
        AddHandler(ProjectsPageView.ApplyWebConfigurationRequestedEvent, new RoutedEventHandler(ApplyWebConfiguration_Click));
        AddHandler(ProjectsPageView.CreateProjectRequestedEvent, new RoutedEventHandler(CreateGeneralProject_Click));
        AddHandler(ProjectsPageView.RegisterExistingProjectRequestedEvent, new RoutedEventHandler(RegisterExistingProject_Click));
        AddHandler(ProjectsPageView.ProjectActionRequestedEvent, new EventHandler<ProjectActionRequestedEventArgs>(ProjectsPage_ProjectActionRequested));
        AddHandler(SchedulerPageView.NewTaskRequestedEvent, new RoutedEventHandler(NewScheduledTask_Click));
        AddHandler(SchedulerPageView.TaskActionRequestedEvent, new EventHandler<ScheduledTaskActionRequestedEventArgs>(SchedulerPage_TaskActionRequested));
        AddHandler(SchedulerPageView.HistoryActionRequestedEvent, new EventHandler<ScheduledTaskHistoryActionRequestedEventArgs>(SchedulerPage_HistoryActionRequested));
        AddHandler(TerminalPageView.SubmitRequestedEvent, new RoutedEventHandler(TerminalPage_SubmitRequested));
        AddHandler(TerminalPageView.CancelRequestedEvent, new RoutedEventHandler(TerminalPage_CancelRequested));
        AddHandler(FilesPageView.InteractionRequestedEvent, new EventHandler<FilesInteractionRequestedEventArgs>(FilesPage_InteractionRequested));
        AddHandler(SeleniumPageView.ActionRequestedEvent, new EventHandler<SeleniumActionRequestedEventArgs>(SeleniumPage_ActionRequested));
        AddHandler(PackageManagerView.OpenProjectRequestedEvent, new EventHandler(PackageManager_OpenProjectRequested));
        AddHandler(PackageManagerView.RefreshRequestedEvent, new EventHandler(PackageManager_RefreshRequested));
        AddHandler(PackageManagerView.InstallRequestedEvent, new EventHandler<PackageInstallRequestedEventArgs>(PackageManager_InstallRequested));
        AddHandler(PackageManagerView.RemoveRequestedEvent, new EventHandler<PackageRemoveRequestedEventArgs>(PackageManager_RemoveRequested));
    }
}
