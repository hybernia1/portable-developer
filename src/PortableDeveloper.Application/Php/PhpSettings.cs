namespace PortableDeveloper.Application.Php;

public sealed record PhpSettings
{
    public int MemoryLimitMb { get; init; } = 256;

    public int UploadMaxFileSizeMb { get; init; } = 128;

    public int PostMaxSizeMb { get; init; } = 128;

    public int MaxExecutionTimeSeconds { get; init; } = 300;

    public int MaxInputVariables { get; init; } = 3000;

    public bool DisplayErrors { get; init; } = true;

    public string[] EnabledExtensions { get; init; } = [.. PhpExtensionCatalog.DefaultEnabledNames];

    public static PhpSettings Default => new();
}

public static class PhpSettingsValidator
{
    public static PhpSettings Normalize(PhpSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ValidateRange(settings.MemoryLimitMb, 32, 8192, nameof(settings.MemoryLimitMb));
        ValidateRange(settings.UploadMaxFileSizeMb, 1, 2048, nameof(settings.UploadMaxFileSizeMb));
        ValidateRange(settings.PostMaxSizeMb, 1, 4096, nameof(settings.PostMaxSizeMb));
        ValidateRange(settings.MaxExecutionTimeSeconds, 0, 3600, nameof(settings.MaxExecutionTimeSeconds));
        ValidateRange(settings.MaxInputVariables, 100, 100000, nameof(settings.MaxInputVariables));
        if (settings.PostMaxSizeMb < settings.UploadMaxFileSizeMb)
        {
            throw new ArgumentException("PHP post_max_size must be greater than or equal to upload_max_filesize.", nameof(settings));
        }

        var requested = new HashSet<string>(settings.EnabledExtensions ?? [], StringComparer.OrdinalIgnoreCase);
        var knownNames = PhpExtensionCatalog.All.Select(extension => extension.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unknown = requested.FirstOrDefault(name => !knownNames.Contains(name));
        if (unknown is not null)
        {
            throw new ArgumentException($"Unsupported PHP extension: {unknown}.", nameof(settings));
        }

        foreach (var required in PhpExtensionCatalog.All.Where(extension => extension.IsRequired))
        {
            requested.Add(required.Name);
        }

        return settings with
        {
            EnabledExtensions = PhpExtensionCatalog.All
                .Where(extension => requested.Contains(extension.Name))
                .Select(extension => extension.Name)
                .ToArray()
        };
    }

    private static void ValidateRange(int value, int minimum, int maximum, string parameterName)
    {
        if (value < minimum || value > maximum)
        {
            throw new ArgumentOutOfRangeException(parameterName, $"Value must be between {minimum} and {maximum}.");
        }
    }
}
