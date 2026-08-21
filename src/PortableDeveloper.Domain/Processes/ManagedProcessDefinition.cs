namespace PortableDeveloper.Domain.Processes;

/// <summary>
/// Describes one external process managed by Portable Developer.
/// All file paths are relative to the portable application root.
/// </summary>
public sealed record ManagedProcessDefinition(
    string Id,
    string ExecutableRelativePath,
    string WorkingDirectoryRelativePath,
    string? Arguments = null,
    IReadOnlyDictionary<string, string>? Environment = null);
