namespace PortableDeveloper.Application.Selenium;

public enum SeleniumProfileBrowser
{
    Edge,
    Chrome,
    Firefox
}

public enum SeleniumProfileLayout
{
    ChromiumUserData,
    FirefoxProfile
}

public enum SeleniumProfileVerificationState
{
    Verified,
    Damaged
}

public sealed record SeleniumProfileInfo(
    string Id,
    string Name,
    SeleniumProfileBrowser Browser,
    string MasterRelativePath,
    DateTimeOffset ImportedAtUtc,
    long ApproximateSizeBytes,
    int FileCount,
    SeleniumProfileLayout Layout,
    string? ChromiumProfileDirectory,
    string BrowserVersion,
    SeleniumProfileVerificationState VerificationState,
    string VerificationDetail)
{
    public bool IsVerified => VerificationState == SeleniumProfileVerificationState.Verified;
}

public sealed record SeleniumProfileOperationResult(
    bool IsSuccess,
    string Detail,
    SeleniumProfileInfo? Profile = null)
{
    public static SeleniumProfileOperationResult Success(SeleniumProfileInfo? profile = null) =>
        new(true, string.Empty, profile);

    public static SeleniumProfileOperationResult Failure(string detail) =>
        new(false, detail);
}
