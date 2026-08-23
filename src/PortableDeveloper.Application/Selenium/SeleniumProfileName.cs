namespace PortableDeveloper.Application.Selenium;

public static class SeleniumProfileName
{
    public static bool TryNormalize(string? name, out string normalized)
    {
        normalized = name?.Trim() ?? string.Empty;
        return normalized.Length is >= 1 and <= 80 && !normalized.Any(char.IsControl);
    }

    public static string Normalize(string? name)
    {
        if (!TryNormalize(name, out var normalized))
        {
            throw new ArgumentException("The profile name must contain 1 to 80 printable characters.", nameof(name));
        }

        return normalized;
    }
}
