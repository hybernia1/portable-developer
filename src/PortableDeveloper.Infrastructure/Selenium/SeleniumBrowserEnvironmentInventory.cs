using System.Diagnostics;
using System.Security.Cryptography;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Packages;
using PortableDeveloper.Application.Selenium;

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
        var drivers = _drivers.ScanAll();
        var candidates = new List<BrowserCandidate>();
        AddPortableChrome(candidates);
        AddSystemCandidates(candidates);

        return candidates
            .Select(candidate => Pair(candidate, drivers))
            .OrderByDescending(environment => environment.IsReady)
            .ThenBy(environment => environment.Source)
            .ThenBy(environment => environment.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    private void AddPortableChrome(ICollection<BrowserCandidate> candidates)
    {
        var component = _catalog.Load().Components.SingleOrDefault(item =>
            string.Equals(item.Id, "chrome-for-testing", StringComparison.OrdinalIgnoreCase));
        if (component?.NormalizedEntrypointRelativePath is null || component.NormalizedEntrypointSha256 is null)
        {
            return;
        }

        var relativePath = Path.Combine(
            "modules", "browsers", "chrome-for-testing", component.Version,
            component.NormalizedEntrypointRelativePath);
        var fullPath = _paths.Resolve(relativePath);
        if (!File.Exists(fullPath) || IsReparsePoint(fullPath)
            || !string.Equals(ComputeSha256(fullPath), component.NormalizedEntrypointSha256, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        candidates.Add(new(
            "portable-chrome-for-testing",
            "chrome",
            "Chrome for Testing (portable)",
            component.Version,
            relativePath,
            true,
            SeleniumBrowserSource.Portable));
    }

    private static void AddSystemCandidates(ICollection<BrowserCandidate> candidates)
    {
        AddFirstExisting(candidates, "system-edge", "MicrosoftEdge", "Microsoft Edge (Windows)", SeleniumBrowserSource.System,
            CandidatePaths("Microsoft/Edge/Application/msedge.exe"));
        AddFirstExisting(candidates, "system-chrome", "chrome", "Google Chrome (Windows)", SeleniumBrowserSource.System,
            CandidatePaths("Google/Chrome/Application/chrome.exe"));
        AddFirstExisting(candidates, "system-firefox", "firefox", "Mozilla Firefox (Windows)", SeleniumBrowserSource.System,
            CandidatePaths("Mozilla Firefox/firefox.exe"));
    }

    private SeleniumBrowserEnvironmentInfo Pair(
        BrowserCandidate browser,
        IReadOnlyList<SeleniumDriverInfo> drivers)
    {
        var matching = drivers
            .Where(driver => string.Equals(driver.BrowserName, browser.BrowserName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(driver => CompatibilityScore(browser, driver))
            .ThenByDescending(driver => ParseVersion(driver.Version))
            .ToArray();
        var compatible = matching.FirstOrDefault(driver => IsCompatible(browser, driver));
        if (compatible is not null)
        {
            return Create(browser, compatible, SeleniumBrowserEnvironmentState.Ready,
                browser.Source == SeleniumBrowserSource.Portable
                    ? "Verified portable browser and compatible driver."
                    : "Detected Windows browser and compatible portable driver.");
        }

        return matching.Length == 0
            ? Create(browser, null, SeleniumBrowserEnvironmentState.DriverMissing,
                "The browser is available, but no matching portable driver is installed.")
            : Create(browser, matching[0], SeleniumBrowserEnvironmentState.VersionMismatch,
                $"Browser {browser.Version} is not compatible with driver {matching[0].Version}.");
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
            browser.IsPortable,
            browser.Source,
            driver,
            state,
            detail);

    private static bool IsCompatible(BrowserCandidate browser, SeleniumDriverInfo driver)
    {
        if (browser.BrowserName.Equals("firefox", StringComparison.OrdinalIgnoreCase))
        {
            return !string.Equals(driver.Version, "unknown", StringComparison.OrdinalIgnoreCase);
        }

        if (!Version.TryParse(browser.Version, out var browserVersion)
            || !Version.TryParse(driver.Version, out var driverVersion))
        {
            return false;
        }

        if (browser.BrowserName.Equals("MicrosoftEdge", StringComparison.OrdinalIgnoreCase))
        {
            return browserVersion.Major == driverVersion.Major
                   && browserVersion.Minor == driverVersion.Minor
                   && browserVersion.Build == driverVersion.Build;
        }

        return browserVersion.Major == driverVersion.Major;
    }

    private static int CompatibilityScore(BrowserCandidate browser, SeleniumDriverInfo driver) =>
        IsCompatible(browser, driver) ? 1 : 0;

    private static void AddFirstExisting(
        ICollection<BrowserCandidate> candidates,
        string id,
        string browserName,
        string displayName,
        SeleniumBrowserSource source,
        IEnumerable<string> paths)
    {
        var path = paths.FirstOrDefault(File.Exists);
        if (path is null || IsReparsePoint(path))
        {
            return;
        }

        candidates.Add(new(id, browserName, displayName, ReadVersion(path), path, false, source));
    }

    private static IEnumerable<string> CandidatePaths(string suffix)
    {
        foreach (var root in new[]
                 {
                     Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                     Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                     Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                 }.Where(path => !string.IsNullOrWhiteSpace(path)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            yield return Path.Combine(root, suffix.Replace('/', Path.DirectorySeparatorChar));
        }
    }

    private static string ReadVersion(string path)
    {
        var version = FileVersionInfo.GetVersionInfo(path).FileVersion?.Split(' ', '-', '+')[0];
        return Version.TryParse(version, out var parsed) ? parsed.ToString() : "unknown";
    }

    private static Version ParseVersion(string version) =>
        Version.TryParse(version, out var parsed) ? parsed : new Version(0, 0);

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
        bool IsPortable,
        SeleniumBrowserSource Source);
}
