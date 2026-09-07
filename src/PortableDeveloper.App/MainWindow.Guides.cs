namespace PortableDeveloper.App;

public partial class MainWindow
{
    private void RefreshGuides(bool resetSearch = false) =>
        _dashboard.GuidesPage.Refresh(
            _dashboard.Runtime.ApachePort,
            _dashboard.Runtime.MariaDbPort,
            _dashboard.Runtime.SeleniumPort,
            resetSearch);
}
