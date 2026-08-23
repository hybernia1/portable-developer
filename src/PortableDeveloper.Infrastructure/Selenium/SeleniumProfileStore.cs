using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Selenium;

namespace PortableDeveloper.Infrastructure.Selenium;

public sealed partial class SeleniumProfileStore : ISeleniumProfileStore
{
    private const string ProfilesRoot = "profiles/selenium";
    private const string ManagedDraftsRoot = "temp/selenium-profile-creation";
    private const string SessionCopiesRoot = "temp/selenium-profiles";
    private const int MaximumProfileFiles = 25_000;
    private const long MaximumProfileBytes = 2L * 1024 * 1024 * 1024;
    private static readonly HashSet<string> SkippedDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Cache", "Code Cache", "GPUCache", "Crashpad", "GrShaderCache", "ShaderCache",
        "DawnGraphiteCache", "DawnWebGPUCache", "minidumps",
        // Firefox reproducible cache and diagnostic roots. Authentication, extensions,
        // site storage, Sync, history, security state, and downloaded codecs stay intact.
        "cache2", "startupCache", "shader-cache", "crashes", "datareporting",
        "saved-telemetry-pings", "thumbnails"
    };
    private static readonly HashSet<string> SkippedFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "SingletonCookie", "SingletonLock", "SingletonSocket", "DevToolsActivePort",
        "parent.lock", ".parentlock", "lock"
    };
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
        RecoverInterruptedEdits();
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

    public SeleniumProfileOperationResult CreateFromManagedDraft(
        string name,
        SeleniumProfileBrowser browser,
        string draftRelativePath,
        string? browserVersion = null)
    {
        try
        {
            var normalizedName = SeleniumProfileName.Normalize(name);
            if (string.IsNullOrWhiteSpace(draftRelativePath))
            {
                return SeleniumProfileOperationResult.Failure("The managed browser profile draft is missing.");
            }

            var source = _paths.Resolve(draftRelativePath);
            var managedDraftsRoot = _paths.Resolve(ManagedDraftsRoot);
            if (!IsChildPath(source, managedDraftsRoot))
            {
                return SeleniumProfileOperationResult.Failure("Profiles can only be created by an app-managed browser.");
            }

            if (!Directory.Exists(source) || IsReparsePoint(source))
            {
                return SeleniumProfileOperationResult.Failure("The managed browser profile draft does not exist or is unsafe.");
            }

            var id = Guid.NewGuid().ToString("N");
            var stagingRelativePath = Path.Combine("temp", "profile-sealing", id);
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
                WriteSealedProfile(
                    staging,
                    source,
                    id,
                    normalizedName,
                    browser,
                    DateTimeOffset.UtcNow,
                    browserVersion);
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
                ?? throw new InvalidDataException("The created profile could not be verified.");
            Log("selenium.profile.created", $"profile={id}; browser={BrowserKey(browser)}");
            return SeleniumProfileOperationResult.Success(profile);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException or JsonException)
        {
            return SeleniumProfileOperationResult.Failure(exception.Message);
        }
    }

    public string CreateEditDraft(string profileId, string draftToken)
    {
        ValidateId(profileId);
        ValidateId(draftToken);
        var profile = GetProfiles().SingleOrDefault(item => item.Id == profileId && item.IsVerified)
            ?? throw new InvalidDataException("The requested Selenium master profile does not exist or is damaged.");
        var targetRelativePath = Path.Combine(ManagedDraftsRoot, draftToken);
        var target = _paths.Resolve(targetRelativePath);
        if (Directory.Exists(target))
        {
            throw new IOException("A managed browser profile draft with the same token already exists.");
        }

        CopyDirectory(_paths.Resolve(profile.MasterRelativePath), target, makeWritable: true);
        Log("selenium.profile.edit-draft.created", $"profile={profileId}; draft={draftToken}");
        return targetRelativePath;
    }

    public SeleniumProfileOperationResult UpdateFromManagedDraft(
        string profileId,
        string draftRelativePath,
        string? browserVersion = null)
    {
        try
        {
            ValidateId(profileId);
            var existing = GetProfiles().SingleOrDefault(item => item.Id == profileId && item.IsVerified);
            if (existing is null)
            {
                return SeleniumProfileOperationResult.Failure("The Selenium master profile does not exist or is damaged.");
            }

            var source = ValidateManagedDraft(draftRelativePath);
            var operationToken = Guid.NewGuid().ToString("N");
            var staging = _paths.Resolve(Path.Combine("temp", "profile-sealing", operationToken));
            var backup = _paths.Resolve(Path.Combine("temp", "profile-backups", $"{profileId}-{operationToken}"));
            var target = _paths.Resolve(Path.Combine(ProfilesRoot, profileId));
            Directory.CreateDirectory(staging);
            try
            {
                WriteSealedProfile(
                    staging,
                    source,
                    profileId,
                    existing.Name,
                    existing.Browser,
                    existing.ImportedAtUtc,
                    browserVersion ?? existing.BrowserVersion);

                Directory.CreateDirectory(Path.GetDirectoryName(backup)!);
                Directory.Move(target, backup);
                try
                {
                    Directory.Move(staging, target);
                }
                catch
                {
                    if (!Directory.Exists(target) && Directory.Exists(backup))
                    {
                        Directory.Move(backup, target);
                    }

                    throw;
                }

                if (Directory.Exists(backup))
                {
                    try
                    {
                        DeleteDirectory(backup);
                    }
                    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
                    {
                        _ = _logger.LogAsync(
                            ApplicationLogLevel.Warning,
                            "selenium-profiles",
                            "selenium.profile.edit-backup.cleanup-failed",
                            $"profile={profileId}; backup={operationToken}; error={exception.Message}");
                    }
                }
            }
            finally
            {
                if (Directory.Exists(staging))
                {
                    DeleteDirectory(staging);
                }
            }

            var profile = ReadProfile(target)
                ?? throw new InvalidDataException("The updated profile could not be verified.");
            Log("selenium.profile.updated", $"profile={profileId}; browser={BrowserKey(existing.Browser)}");
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

    public bool IsManagedDraftInUse(string draftRelativePath)
    {
        if (string.IsNullOrWhiteSpace(draftRelativePath))
        {
            throw new ArgumentException("The managed browser profile draft is missing.", nameof(draftRelativePath));
        }

        var source = _paths.Resolve(draftRelativePath);
        var managedDraftsRoot = _paths.Resolve(ManagedDraftsRoot);
        if (!IsChildPath(source, managedDraftsRoot))
        {
            throw new ArgumentException("Profiles can only be inspected inside app-managed browser storage.", nameof(draftRelativePath));
        }

        if (!Directory.Exists(source))
        {
            return false;
        }

        if (IsReparsePoint(source))
        {
            throw new InvalidDataException("The managed browser profile draft is unsafe.");
        }

        return Directory.EnumerateFiles(source, "*", SearchOption.TopDirectoryOnly)
            .Where(file => SkippedFileNames.Contains(Path.GetFileName(file)))
            .Any(IsFileActivelyLocked);
    }

    public string CreateSessionCopy(string profileId, string sessionToken)
    {
        ValidateId(profileId);
        ValidateId(sessionToken);
        var profile = GetProfiles().SingleOrDefault(item => item.Id == profileId && item.IsVerified)
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

    public void DeleteInactiveManagedDrafts()
    {
        var root = _paths.EnsureDirectory(ManagedDraftsRoot);
        foreach (var directory in Directory.EnumerateDirectories(root, "*", SearchOption.TopDirectoryOnly))
        {
            try
            {
                if (IsReparsePoint(directory))
                {
                    continue;
                }

                EnsureProfileIsNotLocked(directory, SkippedFileNames);
                DeleteDirectory(directory);
                Log("selenium.profile.stale-draft.removed", $"draft={Path.GetFileName(directory)}");
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
            {
                _ = _logger.LogAsync(
                    ApplicationLogLevel.Warning,
                    "selenium-profiles",
                    "selenium.profile.stale-draft.cleanup-skipped",
                    $"draft={Path.GetFileName(directory)}; error={exception.Message}");
            }
        }
    }

    private void RecoverInterruptedEdits()
    {
        var backupRoot = _paths.EnsureDirectory(Path.Combine("temp", "profile-backups"));
        foreach (var backup in Directory.EnumerateDirectories(backupRoot, "*", SearchOption.TopDirectoryOnly))
        {
            try
            {
                if (IsReparsePoint(backup))
                {
                    continue;
                }

                var backupName = Path.GetFileName(backup);
                var separatorIndex = backupName.IndexOf('-');
                if (separatorIndex != 32)
                {
                    continue;
                }

                var profileId = backupName[..separatorIndex];
                ValidateId(profileId);
                var target = _paths.Resolve(Path.Combine(ProfilesRoot, profileId));
                if (!Directory.Exists(target))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    Directory.Move(backup, target);
                    Log("selenium.profile.edit.recovered", $"profile={profileId}");
                }
                else
                {
                    DeleteDirectory(backup);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException)
            {
                _ = _logger.LogAsync(
                    ApplicationLogLevel.Warning,
                    "selenium-profiles",
                    "selenium.profile.edit-recovery.failed",
                    $"backup={Path.GetFileName(backup)}; error={exception.Message}");
            }
        }
    }

    private SeleniumProfileInfo? ReadProfile(string directory)
    {
        ProfileMetadata? metadata = null;
        try
        {
            var metadataPath = Path.Combine(directory, "profile.json");
            var master = Path.Combine(directory, "master");
            if (!File.Exists(metadataPath) || !Directory.Exists(master) || IsReparsePoint(master))
            {
                return null;
            }

            metadata = JsonSerializer.Deserialize<ProfileMetadata>(File.ReadAllText(metadataPath), JsonOptions);
            if (metadata is null || metadata.SchemaVersion is not (1 or 2) || !IdPattern().IsMatch(metadata.Id))
            {
                return null;
            }

            var expectedDirectory = _paths.Resolve(Path.Combine(ProfilesRoot, metadata.Id));
            if (!Path.GetFullPath(directory).Equals(expectedDirectory, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (metadata.SchemaVersion == 1)
            {
                metadata = UpgradeLegacyProfile(directory, master, metadata);
            }

            var manifest = VerifyManifest(directory, master, metadata);
            return new(
                metadata.Id,
                metadata.Name,
                metadata.Browser,
                Path.Combine(ProfilesRoot, metadata.Id, "master"),
                metadata.ImportedAtUtc,
                manifest.TotalBytes,
                manifest.FileCount,
                metadata.Layout!.Value,
                metadata.ChromiumProfileDirectory,
                metadata.BrowserVersion ?? "unknown",
                SeleniumProfileVerificationState.Verified,
                "Profile manifest and layout are verified.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        {
            if (metadata is null || !IdPattern().IsMatch(metadata.Id))
            {
                return null;
            }

            var expectedDirectory = _paths.Resolve(Path.Combine(ProfilesRoot, metadata.Id));
            if (!Path.GetFullPath(directory).Equals(expectedDirectory, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return new(
                metadata.Id,
                metadata.Name,
                metadata.Browser,
                Path.Combine(ProfilesRoot, metadata.Id, "master"),
                metadata.ImportedAtUtc,
                0,
                0,
                metadata.Layout ?? (metadata.Browser == SeleniumProfileBrowser.Firefox
                    ? SeleniumProfileLayout.FirefoxProfile
                    : SeleniumProfileLayout.ChromiumUserData),
                metadata.ChromiumProfileDirectory,
                metadata.BrowserVersion ?? "unknown",
                SeleniumProfileVerificationState.Damaged,
                exception.Message);
        }
    }

    private static void CopyDirectory(
        string source,
        string destination,
        bool makeWritable,
        ImportBudget? budget = null,
        bool skipVolatileItems = false)
    {
        Directory.CreateDirectory(destination);
        foreach (var directory in EnumerateSafeDirectories(source))
        {
            if (skipVolatileItems && IsSkippedDirectory(source, directory))
            {
                continue;
            }

            Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, directory)));
        }

        foreach (var file in EnumerateSafeFiles(source))
        {
            if (skipVolatileItems && (SkippedFileNames.Contains(Path.GetFileName(file)) || IsUnderSkippedDirectory(source, file)))
            {
                continue;
            }

            budget?.Add(new FileInfo(file).Length);
            var target = Path.Combine(destination, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
            if (makeWritable)
            {
                File.SetAttributes(target, File.GetAttributes(target) & ~FileAttributes.ReadOnly);
            }
        }
    }

    private static NormalizedProfile NormalizeAndCopyProfile(
        string source,
        string destination,
        SeleniumProfileBrowser browser)
    {
        var budget = new ImportBudget();
        if (browser == SeleniumProfileBrowser.Firefox)
        {
            EnsureProfileIsNotLocked(source, ["parent.lock", ".parentlock", "lock"]);
            if (!File.Exists(Path.Combine(source, "prefs.js")))
            {
                throw new InvalidDataException("The selected folder is not a Firefox profile (prefs.js is missing).");
            }

            CopyDirectory(source, destination, makeWritable: false, budget, skipVolatileItems: true);
            return new(SeleniumProfileLayout.FirefoxProfile, null);
        }

        string userDataRoot;
        string profileDirectory;
        if (File.Exists(Path.Combine(source, "Local State")))
        {
            userDataRoot = source;
            profileDirectory = FindChromiumProfileDirectory(source)
                ?? throw new InvalidDataException("The Chromium user data folder contains no profile with Preferences.");
        }
        else if (File.Exists(Path.Combine(source, "Preferences"))
                 && Directory.GetParent(source) is { } parent
                 && File.Exists(Path.Combine(parent.FullName, "Local State")))
        {
            userDataRoot = parent.FullName;
            profileDirectory = Path.GetFileName(source);
        }
        else
        {
            throw new InvalidDataException("Select a Chromium user data folder (Local State) or one of its profile folders (Preferences).");
        }

        EnsureProfileIsNotLocked(userDataRoot, ["SingletonLock"]);

        Directory.CreateDirectory(destination);
        var localState = Path.Combine(userDataRoot, "Local State");
        budget.Add(new FileInfo(localState).Length);
        File.Copy(localState, Path.Combine(destination, "Local State"));
        CopyDirectory(
            Path.Combine(userDataRoot, profileDirectory),
            Path.Combine(destination, profileDirectory),
            makeWritable: false,
            budget,
            skipVolatileItems: true);
        return new(SeleniumProfileLayout.ChromiumUserData, profileDirectory);
    }

    private static string? FindChromiumProfileDirectory(string root) =>
        Directory.EnumerateDirectories(root, "*", SearchOption.TopDirectoryOnly)
            .Where(directory => !IsReparsePoint(directory) && File.Exists(Path.Combine(directory, "Preferences")))
            .OrderByDescending(directory => string.Equals(Path.GetFileName(directory), "Default", StringComparison.OrdinalIgnoreCase))
            .ThenBy(directory => Path.GetFileName(directory), StringComparer.OrdinalIgnoreCase)
            .Select(Path.GetFileName)
            .FirstOrDefault();

    private static ManifestSummary WriteManifest(
        string profileRoot,
        string master,
        SeleniumProfileBrowser browser,
        string browserVersion,
        SeleniumProfileLayout layout,
        string? chromiumProfileDirectory)
    {
        var entries = EnumerateSafeFiles(master)
            .Select(file => CreateManifestEntry(master, file))
            .OrderBy(entry => entry.RelativePath, StringComparer.Ordinal)
            .ToArray();
        var totalBytes = entries.Sum(entry => entry.Size);
        if (entries.Length > MaximumProfileFiles || totalBytes > MaximumProfileBytes)
        {
            throw new InvalidDataException("The browser profile exceeds the portable profile safety limits.");
        }

        var builder = new StringBuilder();
        builder.AppendLine("schemaVersion=1");
        builder.AppendLine($"browser={BrowserKey(browser)}");
        builder.AppendLine($"browserVersion={browserVersion}");
        builder.AppendLine($"layout={layout}");
        builder.AppendLine($"profileDirectory={chromiumProfileDirectory ?? string.Empty}");
        builder.AppendLine($"fileCount={entries.Length}");
        builder.AppendLine($"totalBytes={totalBytes}");
        foreach (var entry in entries)
        {
            builder.Append("file=")
                .Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(entry.RelativePath)))
                .Append('|').Append(entry.Size)
                .Append('|').Append(entry.Sha256)
                .AppendLine();
        }

        var path = Path.Combine(profileRoot, "profile.manifest");
        File.WriteAllText(path, builder.ToString(), new UTF8Encoding(false));
        return new(entries.Length, totalBytes, ComputeSha256(path));
    }

    private static ManifestSummary VerifyManifest(string profileRoot, string master, ProfileMetadata metadata)
    {
        var manifestPath = Path.Combine(profileRoot, "profile.manifest");
        if (!File.Exists(manifestPath)
            || string.IsNullOrWhiteSpace(metadata.ManifestSha256)
            || !string.Equals(ComputeSha256(manifestPath), metadata.ManifestSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("The Selenium profile manifest is missing or damaged.");
        }

        var lines = File.ReadAllLines(manifestPath);
        var entryLines = lines.Where(line => line.StartsWith("file=", StringComparison.Ordinal)).ToArray();
        long totalBytes = 0;
        var expectedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in entryLines)
        {
            var parts = line[5..].Split('|');
            if (parts.Length != 3 || !long.TryParse(parts[1], out var size) || size < 0)
            {
                throw new InvalidDataException("The Selenium profile manifest contains an invalid entry.");
            }

            var relativePath = Encoding.UTF8.GetString(Convert.FromBase64String(parts[0]));
            var fullPath = Path.GetFullPath(Path.Combine(master, relativePath));
            var masterPrefix = Path.GetFullPath(master).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(masterPrefix, StringComparison.OrdinalIgnoreCase)
                || !File.Exists(fullPath)
                || IsReparsePoint(fullPath)
                || new FileInfo(fullPath).Length != size
                || !string.Equals(ComputeSha256(fullPath), parts[2], StringComparison.OrdinalIgnoreCase)
                || !expectedPaths.Add(Path.GetRelativePath(master, fullPath)))
            {
                throw new InvalidDataException("A Selenium master profile file does not match its manifest.");
            }

            totalBytes += size;
        }

        var actualPaths = EnumerateSafeFiles(master)
            .Select(file => Path.GetRelativePath(master, file))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!actualPaths.SetEquals(expectedPaths)
            || entryLines.Length > MaximumProfileFiles
            || totalBytes > MaximumProfileBytes)
        {
            throw new InvalidDataException("The Selenium master profile contains unverified files or exceeds safety limits.");
        }

        ValidateNormalizedLayout(master, metadata.Layout, metadata.ChromiumProfileDirectory);
        return new(entryLines.Length, totalBytes, metadata.ManifestSha256);
    }

    private static ProfileMetadata UpgradeLegacyProfile(string directory, string master, ProfileMetadata metadata)
    {
        SeleniumProfileLayout layout;
        string? profileDirectory = null;
        if (metadata.Browser == SeleniumProfileBrowser.Firefox)
        {
            layout = SeleniumProfileLayout.FirefoxProfile;
        }
        else
        {
            layout = SeleniumProfileLayout.ChromiumUserData;
            profileDirectory = FindChromiumProfileDirectory(master);
        }

        ValidateNormalizedLayout(master, layout, profileDirectory);
        var manifest = WriteManifest(
            directory, master, metadata.Browser, metadata.BrowserVersion ?? "unknown", layout, profileDirectory);
        var upgraded = metadata with
        {
            SchemaVersion = 2,
            Layout = layout,
            ChromiumProfileDirectory = profileDirectory,
            BrowserVersion = metadata.BrowserVersion ?? "unknown",
            ManifestSha256 = manifest.Sha256
        };
        File.WriteAllText(Path.Combine(directory, "profile.json"), JsonSerializer.Serialize(upgraded, JsonOptions), new UTF8Encoding(false));
        File.WriteAllText(
            Path.Combine(directory, "profile.properties"),
            $"schemaVersion=2{Environment.NewLine}id={metadata.Id}{Environment.NewLine}browser={BrowserKey(metadata.Browser)}{Environment.NewLine}" +
            $"layout={layout}{Environment.NewLine}profileDirectory={profileDirectory ?? string.Empty}{Environment.NewLine}" +
            $"browserVersion={upgraded.BrowserVersion}{Environment.NewLine}" +
            $"manifestSha256={manifest.Sha256}{Environment.NewLine}",
            new UTF8Encoding(false));
        return upgraded;
    }

    private static void ValidateNormalizedLayout(
        string master,
        SeleniumProfileLayout? layout,
        string? chromiumProfileDirectory)
    {
        if (layout == SeleniumProfileLayout.FirefoxProfile && File.Exists(Path.Combine(master, "prefs.js")))
        {
            return;
        }

        if (layout == SeleniumProfileLayout.ChromiumUserData
            && !string.IsNullOrWhiteSpace(chromiumProfileDirectory)
            && Path.GetFileName(chromiumProfileDirectory) == chromiumProfileDirectory
            && File.Exists(Path.Combine(master, "Local State"))
            && File.Exists(Path.Combine(master, chromiumProfileDirectory, "Preferences")))
        {
            return;
        }

        throw new InvalidDataException("The Selenium master profile layout is not valid for its browser family.");
    }

    private static ManifestEntry CreateManifestEntry(string root, string file) => new(
        Path.GetRelativePath(root, file).Replace(Path.DirectorySeparatorChar, '/'),
        new FileInfo(file).Length,
        ComputeSha256(file));

    private static void EnsureProfileIsNotLocked(string root, IReadOnlyCollection<string> lockNames)
    {
        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.TopDirectoryOnly))
        {
            if (lockNames.Contains(Path.GetFileName(file), StringComparer.OrdinalIgnoreCase)
                && IsFileActivelyLocked(file))
            {
                throw new InvalidDataException("The managed browser profile is still in use. Close the browser before saving it.");
            }
        }
    }

    private static bool IsFileActivelyLocked(string path)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            return false;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return true;
        }
    }

    private static string NormalizeBrowserVersion(string? version) =>
        Version.TryParse(version, out var parsed) ? parsed.ToString() : "unknown";

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static bool IsSkippedDirectory(string root, string directory) =>
        IsUnderSkippedDirectory(root, directory)
        || SkippedDirectoryNames.Contains(Path.GetFileName(directory));

    private static bool IsUnderSkippedDirectory(string root, string path) =>
        Path.GetRelativePath(root, path)
            .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(SkippedDirectoryNames.Contains);

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

    private string ValidateManagedDraft(string draftRelativePath)
    {
        if (string.IsNullOrWhiteSpace(draftRelativePath))
        {
            throw new ArgumentException("The managed browser profile draft is missing.", nameof(draftRelativePath));
        }

        var source = _paths.Resolve(draftRelativePath);
        var managedDraftsRoot = _paths.Resolve(ManagedDraftsRoot);
        if (!IsChildPath(source, managedDraftsRoot))
        {
            throw new ArgumentException("Profiles can only be updated by an app-managed browser.", nameof(draftRelativePath));
        }

        if (!Directory.Exists(source) || IsReparsePoint(source))
        {
            throw new InvalidDataException("The managed browser profile draft does not exist or is unsafe.");
        }

        return source;
    }

    private static void WriteSealedProfile(
        string profileRoot,
        string source,
        string id,
        string name,
        SeleniumProfileBrowser browser,
        DateTimeOffset importedAtUtc,
        string? browserVersion)
    {
        var master = Path.Combine(profileRoot, "master");
        var normalized = NormalizeAndCopyProfile(source, master, browser);
        var normalizedBrowserVersion = NormalizeBrowserVersion(browserVersion);
        var manifest = WriteManifest(
            profileRoot, master, browser, normalizedBrowserVersion,
            normalized.Layout, normalized.ChromiumProfileDirectory);
        var metadata = new ProfileMetadata(
            2,
            id,
            name,
            browser,
            importedAtUtc,
            normalized.Layout,
            normalized.ChromiumProfileDirectory,
            normalizedBrowserVersion,
            manifest.Sha256);
        File.WriteAllText(
            Path.Combine(profileRoot, "profile.json"),
            JsonSerializer.Serialize(metadata, JsonOptions),
            new UTF8Encoding(false));
        File.WriteAllText(
            Path.Combine(profileRoot, "profile.properties"),
            $"schemaVersion=2{Environment.NewLine}id={id}{Environment.NewLine}browser={BrowserKey(browser)}{Environment.NewLine}" +
            $"layout={normalized.Layout}{Environment.NewLine}profileDirectory={normalized.ChromiumProfileDirectory ?? string.Empty}{Environment.NewLine}" +
            $"browserVersion={normalizedBrowserVersion}{Environment.NewLine}" +
            $"manifestSha256={manifest.Sha256}{Environment.NewLine}",
            new UTF8Encoding(false));
        MakeMasterReadOnly(master);
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

    private static bool IsChildPath(string path, string root)
    {
        var prefix = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                     + Path.DirectorySeparatorChar;
        return Path.GetFullPath(path).StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

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
        DateTimeOffset ImportedAtUtc,
        SeleniumProfileLayout? Layout = null,
        string? ChromiumProfileDirectory = null,
        string? BrowserVersion = null,
        string? ManifestSha256 = null);

    private sealed record NormalizedProfile(SeleniumProfileLayout Layout, string? ChromiumProfileDirectory);
    private sealed record ManifestEntry(string RelativePath, long Size, string Sha256);
    private sealed record ManifestSummary(int FileCount, long TotalBytes, string Sha256);

    private sealed class ImportBudget
    {
        private int _files;
        private long _bytes;

        public void Add(long size)
        {
            _files++;
            _bytes += size;
            if (_files > MaximumProfileFiles || _bytes > MaximumProfileBytes)
            {
                throw new InvalidDataException("The browser profile exceeds 25,000 files or 2 GiB.");
            }
        }
    }
}
