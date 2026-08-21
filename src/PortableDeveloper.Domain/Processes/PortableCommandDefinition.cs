namespace PortableDeveloper.Domain.Processes;

/// <summary>
/// Describes a short-lived executable owned by the portable application.
/// </summary>
public sealed record PortableCommandDefinition(
    string Id,
    string ExecutableRelativePath,
    string WorkingDirectoryRelativePath,
    IReadOnlyList<string> Arguments,
    IReadOnlyDictionary<string, string>? Environment = null,
    TimeSpan? Timeout = null);
