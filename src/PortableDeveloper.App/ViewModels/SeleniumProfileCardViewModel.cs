namespace PortableDeveloper.App.ViewModels;

public sealed record SeleniumProfileCardViewModel(
    string Id,
    string Name,
    string Browser,
    string Size,
    string CapabilityValue,
    string Verification);
