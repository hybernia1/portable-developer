namespace PortableDeveloper.Application.ApachePhp;

public sealed record ApacheRuntimeReadiness(
    bool IsReady,
    IReadOnlyList<string> MissingFiles);
