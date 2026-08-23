using System.Security.Cryptography;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Packages;
using PortableDeveloper.Application.Selenium;
using PortableDeveloper.Domain.Packages;

namespace PortableDeveloper.Infrastructure.Selenium;

public sealed class SeleniumBrowserEnvironmentInventory : ISeleniumBrowserEnvironmentInventory
{
    private readonly IPortablePathResolver _paths;
    private readonly IDependencyLockCatalog _catalog;
    private readonly ISeleniumDriverInventory _drivers;

    public SeleniumBrowserEnvironmentInventory(
        IPortablePathResolver paths,
        IDependencyLockCatalog catalog,
        ISeleniumDriverInventory drivers)
    {
        _paths = paths;
        _catalog = catalog;
        _drivers = drivers;
    }

    public IReadOnlyList<SeleniumBrowserEnvironmentInfo> Scan()
    {
        var catalog = _catalog.Load().Components.ToDictionary(component => component.Id, StringComparer.OrdinalIgnoreCase);
        var drivers = _drivers.ScanAll().Where(driver => driver.IsBundled).ToArray();
        var candidates = new List<BrowserCandidate>();
        AddManagedBrowser(candidates, catalog, "chrome-for-testing", "chromedriver", "chrome", "Chrome for Testing");
        AddManagedBrowser(candidates, catalog, "firefox", "geckodriver", "firefox", "Mozilla Firefox");

        return candidates
            .Select(candidate => Pair(candidate, drivers))
            .OrderByDescending(environment => environment.IsReady)
            .ThenBy(environment => environment.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    private void AddManagedBrowser(
        ICollection<BrowserCandidate> candidates,
        IReadOnlyDictionary<string, DependencyLockComponent> catalog,
        string browserComponentId,
        string driverComponentId,
        string browserName,
        string displayName)
    {
        if (!catalog.TryGetValue(browserComponentId, out var browserComponent)
            || !catalog.TryGetValue(driverComponentId, out var driverComponent)
            || browserComponent.NormalizedEntrypointRelativePath is null
            || browserComponent.NormalizedEntrypointSha256 is null)
        {
            return;
        }

        var relativePath = Path.Combine(
            "modules", "browsers", browserComponentId, browserComponent.Version,
            browserComponent.NormalizedEntrypointRelativePath);
        var fullPath = _paths.Resolve(relativePath);
        if (!File.Exists(fullPath) || IsReparsePoint(fullPath)
            || !string.Equals(ComputeSha256(fullPath), browserComponent.NormalizedEntrypointSha256, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        candidates.Add(new(
            $"managed-{browserComponentId}",
            browserName,
            $"{displayName} (managed)",
            browserComponent.Version,
            relativePath,
            driverComponent.Version));
    }

    private SeleniumBrowserEnvironmentInfo Pair(
        BrowserCandidate browser,
        IReadOnlyList<SeleniumDriverInfo> drivers)
    {
        var matching = drivers
            .Where(driver => string.Equals(driver.BrowserName, browser.BrowserName, StringComparison.OrdinalIgnoreCase))
            .Where(driver => string.Equals(driver.Version, browser.DriverVersion, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var compatible = matching.FirstOrDefault();
        if (compatible is not null)
        {
            return Create(browser, compatible, SeleniumBrowserEnvironmentState.Ready,
                "Verified app-managed browser and its catalog-pinned driver.");
        }

        return Create(browser, null, SeleniumBrowserEnvironmentState.DriverMissing,
            $"The managed browser is installed, but its verified driver {browser.DriverVersion} is missing.");
    }

    private SeleniumBrowserEnvironmentInfo Create(
        BrowserCandidate browser,
        SeleniumDriverInfo? driver,
        SeleniumBrowserEnvironmentState state,
        string detail) => new(
            browser.Id,
            browser.BrowserName,
            browser.DisplayName,
            browser.Version,
            browser.ExecutablePath,
            true,
            SeleniumBrowserSource.Managed,
            driver,
            state,
            detail);

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static bool IsReparsePoint(string path) =>
        (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;

    private sealed record BrowserCandidate(
        string Id,
        string BrowserName,
        string DisplayName,
        string Version,
        string ExecutablePath,
        string DriverVersion);
}
