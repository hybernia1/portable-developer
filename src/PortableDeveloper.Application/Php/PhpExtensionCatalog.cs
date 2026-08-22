namespace PortableDeveloper.Application.Php;

public sealed record PhpExtensionDefinition(string Name, bool IsRequired, bool IsEnabledByDefault);

public static class PhpExtensionCatalog
{
    public static IReadOnlyList<PhpExtensionDefinition> All { get; } =
    [
        new("curl", IsRequired: false, IsEnabledByDefault: true),
        new("fileinfo", IsRequired: false, IsEnabledByDefault: true),
        new("gd", IsRequired: false, IsEnabledByDefault: true),
        new("intl", IsRequired: false, IsEnabledByDefault: true),
        new("mbstring", IsRequired: true, IsEnabledByDefault: true),
        new("mysqli", IsRequired: true, IsEnabledByDefault: true),
        new("openssl", IsRequired: true, IsEnabledByDefault: true),
        new("pdo_mysql", IsRequired: false, IsEnabledByDefault: true),
        new("pdo_sqlite", IsRequired: false, IsEnabledByDefault: false),
        new("soap", IsRequired: false, IsEnabledByDefault: false),
        new("sockets", IsRequired: false, IsEnabledByDefault: false),
        new("sqlite3", IsRequired: false, IsEnabledByDefault: false),
        new("xsl", IsRequired: false, IsEnabledByDefault: false),
        new("zip", IsRequired: true, IsEnabledByDefault: true)
    ];

    public static IReadOnlyList<string> DefaultEnabledNames { get; } = All
        .Where(extension => extension.IsEnabledByDefault || extension.IsRequired)
        .Select(extension => extension.Name)
        .ToArray();
}
