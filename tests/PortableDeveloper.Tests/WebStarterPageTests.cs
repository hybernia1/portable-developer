using PortableDeveloper.Infrastructure.Projects;

namespace PortableDeveloper.Tests;

public sealed class WebStarterPageTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public void EnsureCreated_writes_portable_static_page_and_encodes_project_name()
    {
        var created = WebStarterPage.EnsureCreated(_testRoot, "Tea & <Code>");

        var content = File.ReadAllText(Path.Combine(_testRoot, "index.html"));
        Assert.True(created);
        Assert.Contains("Tea &amp; &lt;Code&gt;", content, StringComparison.Ordinal);
        Assert.DoesNotContain("<?php", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnsureCreated_never_overwrites_an_existing_index()
    {
        Directory.CreateDirectory(_testRoot);
        var indexPath = Path.Combine(_testRoot, "index.html");
        File.WriteAllText(indexPath, "preserve me");

        var created = WebStarterPage.EnsureCreated(_testRoot, "Ignored");

        Assert.False(created);
        Assert.Equal("preserve me", File.ReadAllText(indexPath));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }
}
