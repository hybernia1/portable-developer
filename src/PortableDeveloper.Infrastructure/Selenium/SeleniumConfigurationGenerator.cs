using System.Text;
using System.Text.Json;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Selenium;

namespace PortableDeveloper.Infrastructure.Selenium;

public sealed class SeleniumConfigurationGenerator : ISeleniumConfigurationGenerator
{
    private readonly IPortablePathResolver _paths;

    public SeleniumConfigurationGenerator(IPortablePathResolver paths)
    {
        _paths = paths;
    }

    public string Generate(SeleniumServerOptions options, IReadOnlyList<SeleniumBrowserEnvironmentInfo> environments)
    {
        Validate(options);
        var readyEnvironments = environments.Where(environment => environment.IsReady).ToArray();
        if (readyEnvironments.Length == 0)
        {
            throw new InvalidOperationException("No compatible Selenium browser environment was found.");
        }

        var relativeDirectory = Path.Combine("temp", "generated", options.InstanceId, "selenium");
        _paths.EnsureDirectory(relativeDirectory);
        var relativePath = Path.Combine(relativeDirectory, "selenium.toml");
        var builder = new StringBuilder();
        builder.AppendLine("[server]");
        builder.AppendLine("host = \"127.0.0.1\"");
        builder.AppendLine($"port = {options.Port}");
        builder.AppendLine();
        builder.AppendLine("[node]");
        builder.AppendLine("detect-drivers = false");
        builder.AppendLine("selenium-manager = false");
        builder.AppendLine("delete-session-on-ui = true");
        builder.AppendLine("override-max-sessions = true");
        builder.AppendLine($"max-sessions = {options.MaxSessions}");
        builder.AppendLine($"session-timeout = {options.SessionTimeoutSeconds}");

        foreach (var environment in readyEnvironments)
        {
            var driver = environment.Driver!;
            var executablePath = _paths.Resolve(driver.RelativePath).Replace('\\', '/');
            var browserPath = _paths.Resolve(environment.BrowserExecutablePath);
            var optionsKey = environment.BrowserName switch
            {
                "chrome" => "goog:chromeOptions",
                "MicrosoftEdge" => "ms:edgeOptions",
                "firefox" => "moz:firefoxOptions",
                _ => throw new InvalidOperationException($"Unsupported Selenium browser '{environment.BrowserName}'.")
            };
            var stereotypeData = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["browserName"] = environment.BrowserName,
                ["browserVersion"] = environment.BrowserVersion,
                ["platformName"] = "windows",
                [optionsKey] = new Dictionary<string, object> { ["binary"] = browserPath }
            };
            var stereotype = JsonSerializer.Serialize(stereotypeData);
            builder.AppendLine();
            builder.AppendLine("[[node.driver-configuration]]");
            builder.AppendLine($"display-name = \"{EscapeToml(environment.DisplayName)}\"");
            builder.AppendLine($"webdriver-executable = \"{EscapeToml(executablePath)}\"");
            builder.AppendLine($"max-sessions = {options.MaxSessions}");
            builder.AppendLine($"stereotype = \"{EscapeToml(stereotype)}\"");
        }

        File.WriteAllText(_paths.Resolve(relativePath), builder.ToString(), new UTF8Encoding(false));
        return relativePath;
    }

    internal static void Validate(SeleniumServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(options.InstanceId) ||
            options.InstanceId.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '-' and not '_'))
        {
            throw new ArgumentException("Instance ID may only contain ASCII letters, digits, hyphens, and underscores.", nameof(options));
        }

        if (options.Port is < 1024 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Selenium port must be between 1024 and 65535.");
        }

        if (options.MaxSessions is < 1 or > 32)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Maximum sessions must be between 1 and 32.");
        }

        if (options.SessionTimeoutSeconds is < 30 or > 86400)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Session timeout must be between 30 and 86400 seconds.");
        }
    }

    private static string EscapeToml(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("\"", "\\\"", StringComparison.Ordinal)
        .Replace("\r", "\\r", StringComparison.Ordinal)
        .Replace("\n", "\\n", StringComparison.Ordinal);
}
