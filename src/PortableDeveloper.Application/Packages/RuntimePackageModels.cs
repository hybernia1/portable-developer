namespace PortableDeveloper.Application.Packages;

public enum RuntimePackageKind
{
    Apache,
    Php,
    Database,
    Selenium,
    Composer,
    Python,
    Editor,
    PhpMyAdmin,
    SeleniumChromeEnvironment,
    SeleniumFirefoxEnvironment
}

public enum RuntimePackageInstallStage
{
    Preparing,
    Downloading,
    Verifying,
    Extracting,
    Installing,
    Completed
}

public sealed record RuntimePackageInfo(
    RuntimePackageKind Kind,
    string Version,
    bool IsInstalled,
    string Detail,
    IReadOnlyList<string> Components);

public sealed record RuntimePackageInstallProgress(
    RuntimePackageKind Package,
    RuntimePackageInstallStage Stage,
    string ComponentName,
    int Percentage,
    long BytesReceived = 0,
    long? TotalBytes = null,
    int ComponentIndex = 0,
    int ComponentCount = 0);

public sealed record RuntimePackageInstallResult(
    bool Success,
    string Detail);
