namespace PortableDeveloper.Application.Packages;

public enum RuntimePackageKind
{
    WebStack,
    Database,
    Selenium,
    Composer,
    Python,
    Editor,
    PhpMyAdmin
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
    int Percentage);

public sealed record RuntimePackageInstallResult(
    bool Success,
    string Detail);
