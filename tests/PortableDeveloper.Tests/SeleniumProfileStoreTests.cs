using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Selenium;
using PortableDeveloper.Infrastructure.Paths;
using PortableDeveloper.Infrastructure.Selenium;

namespace PortableDeveloper.Tests;

public sealed class SeleniumProfileStoreTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");
    private readonly string _sourceRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperSourceProfile-{Guid.NewGuid():N}");

    [Fact]
    public void Import_keeps_an_immutable_master_and_session_copy_is_disposable()
    {
        Directory.CreateDirectory(Path.Combine(_sourceRoot, "Default"));
        var sourceFile = Path.Combine(_sourceRoot, "Default", "Preferences");
        File.WriteAllText(sourceFile, "original");
        var store = new SeleniumProfileStore(new PortablePathResolver(_testRoot), new SilentLogger());

        var imported = store.Import("Clean Edge", SeleniumProfileBrowser.Edge, _sourceRoot);

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
    public void Remove_deletes_only_the_portable_master()
    {
        Directory.CreateDirectory(_sourceRoot);
        var sourceFile = Path.Combine(_sourceRoot, "prefs.js");
        File.WriteAllText(sourceFile, "source");
        var store = new SeleniumProfileStore(new PortablePathResolver(_testRoot), new SilentLogger());
        var profile = store.Import("Firefox", SeleniumProfileBrowser.Firefox, _sourceRoot).Profile!;

        var result = store.Remove(profile.Id);

        Assert.True(result.IsSuccess, result.Detail);
        Assert.Empty(store.GetProfiles());
        Assert.Equal("source", File.ReadAllText(sourceFile));
    }

    public void Dispose()
    {
        foreach (var path in new[] { _testRoot, _sourceRoot })
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
