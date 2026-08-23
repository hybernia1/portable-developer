using System.Globalization;
using System.IO;
using System.Reflection;
using PortableDeveloper.Application.Settings;

namespace PortableDeveloper.App.Guides;

internal static class BuiltInGuide
{
    public static string Load(
        ApplicationLanguage language,
        int apachePort,
        int mariaDbPort,
        int seleniumPort)
    {
        var languageCode = language == ApplicationLanguage.Czech ? "cs" : "en";
        var resourceName = $"PortableDeveloper.App.Guides.{languageCode}.md";
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidDataException($"The built-in guide resource '{resourceName}' is missing.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd()
            .Replace("{{APACHE_PORT}}", apachePort.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{{MARIADB_PORT}}", mariaDbPort.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{{SELENIUM_PORT}}", seleniumPort.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }
}
