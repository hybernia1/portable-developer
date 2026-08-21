namespace PortableDeveloper.Application.Php;

public sealed record PhpRuntimeReadiness(
    bool IsReady,
    IReadOnlyList<string> MissingFiles);
