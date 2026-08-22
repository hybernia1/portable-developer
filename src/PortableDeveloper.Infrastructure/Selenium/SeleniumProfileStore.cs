using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Selenium;

namespace PortableDeveloper.Infrastructure.Selenium;

public sealed partial class SeleniumProfileStore : ISeleniumProfileStore
{
    private const string ProfilesRoot = "profiles/selenium";
    private const string SessionCopiesRoot = "temp/selenium-profiles";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly IPortablePathResolver _paths;
    private readonly IApplicationLogger _logger;

    public SeleniumProfileStore(IPortablePathResolver paths, IApplicationLogger logger)
    {
        _paths = paths;
        _logger = logger;
    }

    public IReadOnlyList<SeleniumProfileInfo> GetProfiles()
    {
        var root = _paths.EnsureDirectory(ProfilesRoot);
        return Directory.EnumerateDirectories(root)
            .Where(path => !IsReparsePoint(path))
            .Select(ReadProfile)
            .Where(profile => profile is not null)
            .Cast<SeleniumProfileInfo>()
            .OrderBy(profile => profile.Browser)
            .ThenBy(profile => profile.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    public SeleniumProfileOperationResult Import(
        string name,
        SeleniumProfileBrowser browser,
        string sourceDirectory)
    {
        try
        {
            var normalizedName = ValidateName(name);
            if (string.IsNullOrWhiteSpace(sourceDirectory))
            {
                return SeleniumProfileOperationResult.Failure("Select a browser profile directory first.");
            }

            var source = Path.GetFullPath(sourceDirectory);
            if (!Directory.Exists(source) || IsReparsePoint(source))
            {
                return SeleniumProfileOperationResult.Failure("The selected browser profile directory does not exist or is a reparse point.");
            }

            var id = Guid.NewGuid().ToString("N");
            var stagingRelativePath = Path.Combine("temp", "profile-imports", id);
            var staging = _paths.Resolve(stagingRelativePath);
            var targetRelativePath = Path.Combine(ProfilesRoot, id);
            var target = _paths.Resolve(targetRelativePath);
            var sourcePrefix = source.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                               + Path.DirectorySeparatorChar;
            if (staging.StartsWith(sourcePrefix, StringComparison.OrdinalIgnoreCase)
                || target.StartsWith(sourcePrefix, StringComparison.OrdinalIgnoreCase))
            {
                return SeleniumProfileOperationResult.Failure("The selected source directory cannot contain Portable Developer profile or staging storage.");
            }

            Directory.CreateDirectory(staging);
            try
            {
                var master = Path.Combine(staging, "master");
                CopyDirectory(source, master, makeWritable: false);
                var metadata = new ProfileMetadata(1, id, normalizedName, browser, DateTimeOffset.UtcNow);
                File.WriteAllText(
                    Path.Combine(staging, "profile.json"),
                    JsonSerializer.Serialize(metadata, JsonOptions),
                    new UTF8Encoding(false));
                File.WriteAllText(
                    Path.Combine(staging, "profile.properties"),
                    $"schemaVersion=1{Environment.NewLine}id={id}{Environment.NewLine}browser={BrowserKey(browser)}{Environment.NewLine}",
                    new UTF8Encoding(false));
                MakeMasterReadOnly(master);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                Directory.Move(staging, target);
            }
            finally
            {
                if (Directory.Exists(staging))
                {
                    DeleteDirectory(staging);
                }
            }

            var profile = ReadProfile(target)
                ?? throw new InvalidDataException("The imported profile could not be verified.");
            Log("selenium.profile.imported", $"profile={id}; browser={BrowserKey(browser)}");
            return SeleniumProfileOperationResult.Success(profile);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException or JsonException)
        {
            return SeleniumProfileOperationResult.Failure(exception.Message);
        }
    }

    public SeleniumProfileOperationResult Remove(string id)
    {
        try
        {
            ValidateId(id);
            var path = _paths.Resolve(Path.Combine(ProfilesRoot, id));
            if (!Directory.Exists(path))
            {
                return SeleniumProfileOperationResult.Failure("The Selenium profile does not exist.");
            }

            DeleteDirectory(path);
            Log("selenium.profile.removed", $"profile={id}");
            return SeleniumProfileOperationResult.Success();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException)
        {
            return SeleniumProfileOperationResult.Failure(exception.Message);
        }
    }

    public string CreateSessionCopy(string profileId, string sessionToken)
    {
        ValidateId(profileId);
        ValidateId(sessionToken);
        var profile = GetProfiles().SingleOrDefault(item => item.Id == profileId)
            ?? throw new InvalidDataException("The requested Selenium master profile does not exist.");
        var targetRelativePath = Path.Combine(SessionCopiesRoot, sessionToken);
        var target = _paths.Resolve(targetRelativePath);
        if (Directory.Exists(target))
        {
            throw new IOException("A Selenium session profile copy with the same token already exists.");
        }

        CopyDirectory(_paths.Resolve(profile.MasterRelativePath), target, makeWritable: true);
        return targetRelativePath;
    }

    public void DeleteSessionCopy(string sessionToken)
    {
        ValidateId(sessionToken);
        var path = _paths.Resolve(Path.Combine(SessionCopiesRoot, sessionToken));
        if (Directory.Exists(path))
        {
            DeleteDirectory(path);
        }
    }

    public void DeleteAllSessionCopies()
    {
        var root = _paths.EnsureDirectory(SessionCopiesRoot);
        foreach (var directory in Directory.EnumerateDirectories(root, "*", SearchOption.TopDirectoryOnly))
        {
            try
            {
                if (IsReparsePoint(directory))
                {
                    continue;
                }

                DeleteDirectory(directory);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
            {
                _ = _logger.LogAsync(
                    ApplicationLogLevel.Warning,
                    "selenium-profiles",
                    "selenium.profile.stale-copy.cleanup-failed",
                    $"copy={Path.GetFileName(directory)}; error={exception.Message}");
            }
        }
    }

    private SeleniumProfileInfo? ReadProfile(string directory)
    {
        try
        {
            var metadataPath = Path.Combine(directory, "profile.json");
            var master = Path.Combine(directory, "master");
            if (!File.Exists(metadataPath) || !Directory.Exists(master) || IsReparsePoint(master))
            {
                return null;
            }

            var metadata = JsonSerializer.Deserialize<ProfileMetadata>(File.ReadAllText(metadataPath), JsonOptions);
            if (metadata is null || metadata.SchemaVersion != 1 || !IdPattern().IsMatch(metadata.Id))
            {
                return null;
            }

            var expectedDirectory = _paths.Resolve(Path.Combine(ProfilesRoot, metadata.Id));
            if (!Path.GetFullPath(directory).Equals(expectedDirectory, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var size = EnumerateSafeFiles(master).Sum(file => new FileInfo(file).Length);
            return new(
                metadata.Id,
                metadata.Name,
                metadata.Browser,
                Path.Combine(ProfilesRoot, metadata.Id, "master"),
                metadata.ImportedAtUtc,
                size);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        {
            return null;
        }
    }

    private static void CopyDirectory(string source, string destination, bool makeWritable)
    {
        Directory.CreateDirectory(destination);
        foreach (var directory in EnumerateSafeDirectories(source))
        {
            Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, directory)));
        }

        foreach (var file in EnumerateSafeFiles(source))
        {
            var target = Path.Combine(destination, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
            if (makeWritable)
            {
                File.SetAttributes(target, File.GetAttributes(target) & ~FileAttributes.ReadOnly);
            }
        }
    }

    private static IEnumerable<string> EnumerateSafeFiles(string root)
    {
        foreach (var directory in new[] { root }.Concat(EnumerateSafeDirectories(root)))
        {
            foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly))
            {
                if (IsReparsePoint(file))
                {
                    throw new InvalidDataException("Browser profiles containing symbolic links or reparse points are not supported.");
                }

                yield return file;
            }
        }
    }

    private static IEnumerable<string> EnumerateSafeDirectories(string root)
    {
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            foreach (var directory in Directory.EnumerateDirectories(current, "*", SearchOption.TopDirectoryOnly))
            {
                if (IsReparsePoint(directory))
                {
                    throw new InvalidDataException("Browser profiles containing symbolic links or reparse points are not supported.");
                }

                yield return directory;
                pending.Push(directory);
            }
        }
    }

    private static void MakeMasterReadOnly(string root)
    {
        foreach (var file in EnumerateSafeFiles(root))
        {
            File.SetAttributes(file, File.GetAttributes(file) | FileAttributes.ReadOnly);
        }
    }

    private static void DeleteDirectory(string path)
    {
        var directories = EnumerateSafeDirectories(path).ToArray();
        foreach (var file in EnumerateSafeFiles(path))
        {
            if (IsReparsePoint(file))
            {
                throw new InvalidDataException("Refusing to remove a Selenium profile containing a reparse point.");
            }

            File.SetAttributes(file, File.GetAttributes(file) & ~FileAttributes.ReadOnly);
        }

        _ = directories;

        Directory.Delete(path, recursive: true);
    }

    private static string ValidateName(string name)
    {
        var value = name.Trim();
        if (value.Length is < 1 or > 80 || value.Any(char.IsControl))
        {
            throw new ArgumentException("The profile name must contain 1 to 80 printable characters.", nameof(name));
        }

        return value;
    }

    private static void ValidateId(string id)
    {
        if (!IdPattern().IsMatch(id))
        {
            throw new ArgumentException("The Selenium profile identifier is invalid.", nameof(id));
        }
    }

    private static bool IsReparsePoint(string path) =>
        (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;

    private static string BrowserKey(SeleniumProfileBrowser browser) => browser switch
    {
        SeleniumProfileBrowser.Edge => "MicrosoftEdge",
        SeleniumProfileBrowser.Chrome => "chrome",
        SeleniumProfileBrowser.Firefox => "firefox",
        _ => throw new ArgumentOutOfRangeException(nameof(browser))
    };

    private void Log(string eventName, string message) =>
        _ = _logger.LogAsync(ApplicationLogLevel.Information, "selenium-profiles", eventName, message);

    [GeneratedRegex("^[a-fA-F0-9]{32}$", RegexOptions.CultureInvariant)]
    private static partial Regex IdPattern();

    private sealed record ProfileMetadata(
        int SchemaVersion,
        string Id,
        string Name,
        SeleniumProfileBrowser Browser,
        DateTimeOffset ImportedAtUtc);
}
