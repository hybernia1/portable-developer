using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Selenium;
using PortableDeveloper.Infrastructure.Paths;
using PortableDeveloper.Infrastructure.Selenium;

namespace PortableDeveloper.Tests;

public sealed class SeleniumProfileStoreTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");
    private readonly string _sourceRelativePath = Path.Combine("temp", "selenium-profile-creation", Guid.NewGuid().ToString("N"));
    private string SourceRoot => Path.Combine(_testRoot, _sourceRelativePath);

    [Fact]
    public void Create_keeps_an_immutable_master_and_session_copy_is_disposable()
    {
        Directory.CreateDirectory(Path.Combine(SourceRoot, "Default"));
        File.WriteAllText(Path.Combine(SourceRoot, "Local State"), "{}");
        var sourceFile = Path.Combine(SourceRoot, "Default", "Preferences");
        File.WriteAllText(sourceFile, "original");
        var store = new SeleniumProfileStore(new PortablePathResolver(_testRoot), new SilentLogger());

        var imported = store.CreateFromManagedDraft("Clean Chrome", SeleniumProfileBrowser.Chrome, _sourceRelativePath);

        Assert.True(imported.IsSuccess, imported.Detail);
        var profile = Assert.Single(store.GetProfiles());
        var masterFile = Path.Combine(_testRoot, profile.MasterRelativePath, "Default", "Preferences");
        Assert.True(File.GetAttributes(masterFile).HasFlag(FileAttributes.ReadOnly));
        var sessionToken = Guid.NewGuid().ToString("N");
        var copyRelativePath = store.CreateSessionCopy(profile.Id, sessionToken);
        var copyFile = Path.Combine(_testRoot, copyRelativePath, "Default", "Preferences");
        Assert.False(File.GetAttributes(copyFile).HasFlag(FileAttributes.ReadOnly));
        File.WriteAllText(copyFile, "session change");
        Assert.Equal("original", File.ReadAllText(masterFile));

        store.DeleteSessionCopy(sessionToken);

        Assert.False(Directory.Exists(Path.Combine(_testRoot, copyRelativePath)));
        Assert.Equal("original", File.ReadAllText(sourceFile));
    }

    [Fact]
    public void Tampered_master_is_not_returned_or_copied()
    {
        Directory.CreateDirectory(Path.Combine(SourceRoot, "Default"));
        File.WriteAllText(Path.Combine(SourceRoot, "Local State"), "{}");
        File.WriteAllText(Path.Combine(SourceRoot, "Default", "Preferences"), "original");
        var store = new SeleniumProfileStore(new PortablePathResolver(_testRoot), new SilentLogger());
        var imported = store.CreateFromManagedDraft("Clean Chrome", SeleniumProfileBrowser.Chrome, _sourceRelativePath).Profile!;
        var masterFile = Path.Combine(_testRoot, imported.MasterRelativePath, "Default", "Preferences");
        File.SetAttributes(masterFile, File.GetAttributes(masterFile) & ~FileAttributes.ReadOnly);
        File.WriteAllText(masterFile, "tampered");

        var damaged = Assert.Single(store.GetProfiles());
        Assert.Equal(SeleniumProfileVerificationState.Damaged, damaged.VerificationState);
        Assert.Throws<InvalidDataException>(() => store.CreateSessionCopy(imported.Id, Guid.NewGuid().ToString("N")));
    }

    [Fact]
    public void Edit_reseals_working_copy_and_preserves_profile_identity()
    {
        Directory.CreateDirectory(Path.Combine(SourceRoot, "Default"));
        File.WriteAllText(Path.Combine(SourceRoot, "Local State"), "{}");
        File.WriteAllText(Path.Combine(SourceRoot, "Default", "Preferences"), "original");
        var store = new SeleniumProfileStore(new PortablePathResolver(_testRoot), new SilentLogger());
        var original = store.CreateFromManagedDraft("Automation account", SeleniumProfileBrowser.Chrome, _sourceRelativePath).Profile!;
        var editToken = Guid.NewGuid().ToString("N");

        var editRelativePath = store.CreateEditDraft(original.Id, editToken);
        var editedPreferences = Path.Combine(_testRoot, editRelativePath, "Default", "Preferences");
        Assert.False(File.GetAttributes(editedPreferences).HasFlag(FileAttributes.ReadOnly));
        File.WriteAllText(editedPreferences, "updated login state");
        var result = store.UpdateFromManagedDraft(original.Id, editRelativePath, "123.4");

        Assert.True(result.IsSuccess, result.Detail);
        Assert.Equal(original.Id, result.Profile!.Id);
        Assert.Equal(original.Name, result.Profile.Name);
        Assert.Equal(original.ImportedAtUtc, result.Profile.ImportedAtUtc);
        Assert.Equal("123.4", result.Profile.BrowserVersion);
        var masterPreferences = Path.Combine(_testRoot, result.Profile.MasterRelativePath, "Default", "Preferences");
        Assert.Equal("updated login state", File.ReadAllText(masterPreferences));
        Assert.True(File.GetAttributes(masterPreferences).HasFlag(FileAttributes.ReadOnly));
    }

    [Fact]
    public void Failed_edit_leaves_original_master_verified_and_unchanged()
    {
        Directory.CreateDirectory(Path.Combine(SourceRoot, "Default"));
        File.WriteAllText(Path.Combine(SourceRoot, "Local State"), "{}");
        File.WriteAllText(Path.Combine(SourceRoot, "Default", "Preferences"), "original");
        var store = new SeleniumProfileStore(new PortablePathResolver(_testRoot), new SilentLogger());
        var original = store.CreateFromManagedDraft("Automation account", SeleniumProfileBrowser.Chrome, _sourceRelativePath).Profile!;
        var editRelativePath = store.CreateEditDraft(original.Id, Guid.NewGuid().ToString("N"));
        File.Delete(Path.Combine(_testRoot, editRelativePath, "Local State"));

        var result = store.UpdateFromManagedDraft(original.Id, editRelativePath, "123.4");

        Assert.False(result.IsSuccess);
        var preserved = Assert.Single(store.GetProfiles());
        Assert.True(preserved.IsVerified, preserved.VerificationDetail);
        Assert.Equal(original.Id, preserved.Id);
        Assert.Equal("original", File.ReadAllText(Path.Combine(_testRoot, preserved.MasterRelativePath, "Default", "Preferences")));
    }

    [Fact]
    public void Profile_inventory_recovers_original_master_from_interrupted_edit_backup()
    {
        Directory.CreateDirectory(Path.Combine(SourceRoot, "Default"));
        File.WriteAllText(Path.Combine(SourceRoot, "Local State"), "{}");
        File.WriteAllText(Path.Combine(SourceRoot, "Default", "Preferences"), "original");
        var store = new SeleniumProfileStore(new PortablePathResolver(_testRoot), new SilentLogger());
        var original = store.CreateFromManagedDraft("Automation account", SeleniumProfileBrowser.Chrome, _sourceRelativePath).Profile!;
        var target = Path.Combine(_testRoot, "profiles", "selenium", original.Id);
        var backup = Path.Combine(_testRoot, "temp", "profile-backups", $"{original.Id}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.GetDirectoryName(backup)!);
        Directory.Move(target, backup);

        var recovered = Assert.Single(store.GetProfiles());

        Assert.Equal(original.Id, recovered.Id);
        Assert.True(recovered.IsVerified, recovered.VerificationDetail);
        Assert.True(Directory.Exists(target));
        Assert.False(Directory.Exists(backup));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Profile_name_validation_rejects_empty_value_before_enrollment(string? value)
    {
        Assert.False(SeleniumProfileName.TryNormalize(value, out _));
    }

    [Fact]
    public void Profile_name_validation_trims_valid_value()
    {
        Assert.True(SeleniumProfileName.TryNormalize("  Work account  ", out var normalized));
        Assert.Equal("Work account", normalized);
    }

    [Fact]
    public void Create_rejects_folder_that_is_not_a_browser_profile()
    {
        Directory.CreateDirectory(SourceRoot);
        File.WriteAllText(Path.Combine(SourceRoot, "notes.txt"), "not a profile");
        var store = new SeleniumProfileStore(new PortablePathResolver(_testRoot), new SilentLogger());

        var result = store.CreateFromManagedDraft("Invalid", SeleniumProfileBrowser.Chrome, _sourceRelativePath);

        Assert.False(result.IsSuccess);
        Assert.Contains("Chromium", result.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_rejects_profile_folder_outside_managed_drafts()
    {
        var unmanagedRelativePath = Path.Combine("instances", "default", "browser-profile");
        var unmanagedRoot = Path.Combine(_testRoot, unmanagedRelativePath);
        Directory.CreateDirectory(Path.Combine(unmanagedRoot, "Default"));
        File.WriteAllText(Path.Combine(unmanagedRoot, "Local State"), "{}");
        var store = new SeleniumProfileStore(new PortablePathResolver(_testRoot), new SilentLogger());

        var result = store.CreateFromManagedDraft("Unmanaged", SeleniumProfileBrowser.Chrome, unmanagedRelativePath);

        Assert.False(result.IsSuccess);
        Assert.Contains("app-managed browser", result.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Remove_deletes_only_the_portable_master()
    {
        Directory.CreateDirectory(SourceRoot);
        var sourceFile = Path.Combine(SourceRoot, "prefs.js");
        File.WriteAllText(sourceFile, "source");
        var store = new SeleniumProfileStore(new PortablePathResolver(_testRoot), new SilentLogger());
        var profile = store.CreateFromManagedDraft("Firefox", SeleniumProfileBrowser.Firefox, _sourceRelativePath).Profile!;

        var result = store.Remove(profile.Id);

        Assert.True(result.IsSuccess, result.Detail);
        Assert.Empty(store.GetProfiles());
        Assert.Equal("source", File.ReadAllText(sourceFile));
    }

    [Fact]
    public void Create_accepts_stale_firefox_lock_file_and_excludes_it_from_master()
    {
        Directory.CreateDirectory(SourceRoot);
        File.WriteAllText(Path.Combine(SourceRoot, "prefs.js"), "user_pref(\"test\", true);");
        File.WriteAllText(Path.Combine(SourceRoot, "parent.lock"), string.Empty);
        var store = new SeleniumProfileStore(new PortablePathResolver(_testRoot), new SilentLogger());

        var result = store.CreateFromManagedDraft("Firefox", SeleniumProfileBrowser.Firefox, _sourceRelativePath);

        Assert.True(result.IsSuccess, result.Detail);
        Assert.NotNull(result.Profile);
        Assert.False(File.Exists(Path.Combine(_testRoot, result.Profile.MasterRelativePath, "parent.lock")));
    }

    [Fact]
    public void Create_excludes_only_reproducible_firefox_cache_and_diagnostics()
    {
        Directory.CreateDirectory(SourceRoot);
        File.WriteAllText(Path.Combine(SourceRoot, "prefs.js"), "user_pref(\"test\", true);");
        foreach (var relativePath in new[]
                 {
                     "cache2/entries/cache.bin",
                     "startupCache/scriptCache.bin",
                     "shader-cache/shader.bin",
                     "crashes/pending/crash.dmp",
                     "datareporting/session-state.json",
                     "saved-telemetry-pings/ping.json",
                     "thumbnails/page.png"
                 })
        {
            var path = Path.Combine(SourceRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, "reproducible");
        }

        foreach (var relativePath in new[]
                 {
                     "cookies.sqlite",
                     "key4.db",
                     "logins.json",
                     "extensions/example@example.test.xpi",
                     "storage/default/https+++example.test/idb.sqlite",
                     "weave/failed/records.json",
                     "security_state/crlite.filter",
                     "safebrowsing/google4/filter.bin",
                     "gmp-widevinecdm/1.0/widevinecdm.dll"
                 })
        {
            var path = Path.Combine(SourceRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, "persistent");
        }

        var store = new SeleniumProfileStore(new PortablePathResolver(_testRoot), new SilentLogger());

        var result = store.CreateFromManagedDraft("Firefox", SeleniumProfileBrowser.Firefox, _sourceRelativePath);

        Assert.True(result.IsSuccess, result.Detail);
        var master = Path.Combine(_testRoot, result.Profile!.MasterRelativePath);
        Assert.False(Directory.Exists(Path.Combine(master, "cache2")));
        Assert.False(Directory.Exists(Path.Combine(master, "startupCache")));
        Assert.False(Directory.Exists(Path.Combine(master, "shader-cache")));
        Assert.False(Directory.Exists(Path.Combine(master, "crashes")));
        Assert.False(Directory.Exists(Path.Combine(master, "datareporting")));
        Assert.False(Directory.Exists(Path.Combine(master, "saved-telemetry-pings")));
        Assert.False(Directory.Exists(Path.Combine(master, "thumbnails")));
        Assert.True(File.Exists(Path.Combine(master, "cookies.sqlite")));
        Assert.True(File.Exists(Path.Combine(master, "key4.db")));
        Assert.True(File.Exists(Path.Combine(master, "logins.json")));
        Assert.True(File.Exists(Path.Combine(master, "extensions", "example@example.test.xpi")));
        Assert.True(File.Exists(Path.Combine(master, "storage", "default", "https+++example.test", "idb.sqlite")));
        Assert.True(File.Exists(Path.Combine(master, "weave", "failed", "records.json")));
        Assert.True(File.Exists(Path.Combine(master, "security_state", "crlite.filter")));
        Assert.True(File.Exists(Path.Combine(master, "safebrowsing", "google4", "filter.bin")));
        Assert.True(File.Exists(Path.Combine(master, "gmp-widevinecdm", "1.0", "widevinecdm.dll")));
    }

    [Fact]
    public void Create_rejects_actively_locked_firefox_profile()
    {
        Directory.CreateDirectory(SourceRoot);
        File.WriteAllText(Path.Combine(SourceRoot, "prefs.js"), "user_pref(\"test\", true);");
        var lockPath = Path.Combine(SourceRoot, "parent.lock");
        File.WriteAllText(lockPath, string.Empty);
        using var lockStream = new FileStream(lockPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        var store = new SeleniumProfileStore(new PortablePathResolver(_testRoot), new SilentLogger());

        var result = store.CreateFromManagedDraft("Firefox", SeleniumProfileBrowser.Firefox, _sourceRelativePath);

        Assert.False(result.IsSuccess);
        Assert.Contains("still in use", result.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(store.GetProfiles());
    }

    [Fact]
    public void Managed_draft_usage_distinguishes_stale_and_active_firefox_lock()
    {
        Directory.CreateDirectory(SourceRoot);
        var lockPath = Path.Combine(SourceRoot, "parent.lock");
        File.WriteAllText(lockPath, string.Empty);
        var store = new SeleniumProfileStore(new PortablePathResolver(_testRoot), new SilentLogger());

        Assert.False(store.IsManagedDraftInUse(_sourceRelativePath));
        using (var lockStream = new FileStream(lockPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            Assert.True(store.IsManagedDraftInUse(_sourceRelativePath));
        }
        Assert.False(store.IsManagedDraftInUse(_sourceRelativePath));
    }

    [Fact]
    public void Managed_draft_usage_rejects_path_outside_enrollment_storage()
    {
        var store = new SeleniumProfileStore(new PortablePathResolver(_testRoot), new SilentLogger());

        Assert.Throws<ArgumentException>(() => store.IsManagedDraftInUse(Path.Combine("instances", "default")));
    }

    [Fact]
    public void Startup_cleanup_removes_inactive_draft_with_stale_lock()
    {
        Directory.CreateDirectory(SourceRoot);
        File.WriteAllText(Path.Combine(SourceRoot, "prefs.js"), "user_pref(\"test\", true);");
        File.WriteAllText(Path.Combine(SourceRoot, "parent.lock"), string.Empty);
        var store = new SeleniumProfileStore(new PortablePathResolver(_testRoot), new SilentLogger());

        store.DeleteInactiveManagedDrafts();

        Assert.False(Directory.Exists(SourceRoot));
    }

    [Fact]
    public void Startup_cleanup_preserves_draft_held_by_running_browser()
    {
        Directory.CreateDirectory(SourceRoot);
        File.WriteAllText(Path.Combine(SourceRoot, "prefs.js"), "user_pref(\"test\", true);");
        var lockPath = Path.Combine(SourceRoot, "parent.lock");
        File.WriteAllText(lockPath, string.Empty);
        using var lockStream = new FileStream(lockPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        var store = new SeleniumProfileStore(new PortablePathResolver(_testRoot), new SilentLogger());

        store.DeleteInactiveManagedDrafts();

        Assert.True(Directory.Exists(SourceRoot));
        Assert.True(File.Exists(lockPath));
    }

    public void Dispose()
    {
        foreach (var path in new[] { _testRoot })
        {
            if (Directory.Exists(path))
            {
                foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    File.SetAttributes(file, File.GetAttributes(file) & ~FileAttributes.ReadOnly);
                }
                Directory.Delete(path, recursive: true);
            }
        }
    }

    private sealed class SilentLogger : IApplicationLogger
    {
        public ValueTask LogAsync(
            ApplicationLogLevel level,
            string component,
            string eventName,
            string message,
            CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }
}
