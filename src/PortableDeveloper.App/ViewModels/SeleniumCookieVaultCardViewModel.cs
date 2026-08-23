namespace PortableDeveloper.App.ViewModels;

public sealed record SeleniumCookieVaultCardViewModel(
    string Id,
    string Name,
    string Domains,
    string CookieCount,
    string CapabilityValue,
    string Status);
