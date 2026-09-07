namespace PortableDeveloper.App.ViewModels;

public sealed record ServiceCardViewModel(
    string Name,
    string Description,
    string Detail,
    string State,
    string? ActionKey = null,
    string? ActionLabel = null,
    bool IsActionEnabled = true,
    string Version = "")
{
    public bool HasAction => !string.IsNullOrWhiteSpace(ActionKey);
}
