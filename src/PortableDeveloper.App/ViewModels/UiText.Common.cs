namespace PortableDeveloper.App.ViewModels;

public sealed partial class UiText
{
    public string OverviewTab => IsCzech ? "Přehled" : "Overview";

    public string SettingsTab => IsCzech ? "Nastavení" : "Settings";

    public string SettingsGeneralTab => IsCzech ? "Obecné" : "General";

    public string SettingsStorageTab => IsCzech ? "Úložiště" : "Storage";

    public string SettingsAboutTab => IsCzech ? "O aplikaci" : "About";

    public string ExtensionsTab => IsCzech ? "Rozšíření" : "Extensions";

    public string DatabasesTab => IsCzech ? "Databáze" : "Databases";

    public string AdministrationTab => IsCzech ? "Webová správa" : "Web administration";

}
