namespace PortableDeveloper.Application.Selenium;

public sealed record SeleniumCookieVaultInfo(
    string Id,
    string Name,
    int CookieCount,
    IReadOnlyList<string> Domains,
    DateTimeOffset ImportedAtUtc,
    bool IsDamaged,
    string Detail);

public sealed record SeleniumCookieVaultOperationResult(
    bool IsSuccess,
    string Detail,
    SeleniumCookieVaultInfo? Vault = null,
    int SkippedCookies = 0)
{
    public static SeleniumCookieVaultOperationResult Success(
        SeleniumCookieVaultInfo? vault = null,
        int skippedCookies = 0) =>
        new(true, string.Empty, vault, skippedCookies);

    public static SeleniumCookieVaultOperationResult Failure(string detail) =>
        new(false, detail);
}
