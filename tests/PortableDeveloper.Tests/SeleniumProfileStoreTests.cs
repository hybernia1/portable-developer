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
