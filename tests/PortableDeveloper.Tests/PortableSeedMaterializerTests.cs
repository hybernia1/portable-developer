using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PortableDeveloper.Infrastructure.Bootstrap;
using PortableDeveloper.Infrastructure.Paths;

namespace PortableDeveloper.Tests;

public sealed class PortableSeedMaterializerTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperSeedTests-{Guid.NewGuid():N}");

    [Fact]
    public void EnsureInitialized_materializes_seed_and_creates_portable_roots()
    {
        Directory.CreateDirectory(_testRoot);
        var materializer = CreateMaterializer();
        using var archive = CreateSeed(("catalog/modules.json", "modules"), ("docs/LICENSE", "license"));

        materializer.EnsureInitialized(archive);

        Assert.Equal("modules", File.ReadAllText(Path.Combine(_testRoot, "catalog", "modules.json")));
        Assert.Equal("license", File.ReadAllText(Path.Combine(_testRoot, "docs", "LICENSE")));
        Assert.True(File.Exists(Path.Combine(_testRoot, "state", "portable-seed.json")));
        Assert.True(Directory.Exists(Path.Combine(_testRoot, "instances")));
        Assert.True(Directory.Exists(Path.Combine(_testRoot, "profiles")));
        Assert.True(Directory.Exists(Path.Combine(_testRoot, "downloads")));
    }

    [Fact]
    public void EnsureInitialized_repairs_owned_files_and_preserves_user_data()
    {
        Directory.CreateDirectory(Path.Combine(_testRoot, "catalog"));
        Directory.CreateDirectory(Path.Combine(_testRoot, "instances", "personal"));
        File.WriteAllText(Path.Combine(_testRoot, "catalog", "modules.json"), "damaged");
        File.WriteAllText(Path.Combine(_testRoot, "instances", "personal", "keep.txt"), "mine");
        var materializer = CreateMaterializer();
        using var archive = CreateSeed(("catalog/modules.json", "verified"));

        materializer.EnsureInitialized(archive);

        Assert.Equal("verified", File.ReadAllText(Path.Combine(_testRoot, "catalog", "modules.json")));
        Assert.Equal("mine", File.ReadAllText(Path.Combine(_testRoot, "instances", "personal", "keep.txt")));
    }

    [Fact]
    public void EnsureInitialized_is_idempotent()
    {
        Directory.CreateDirectory(_testRoot);
        var materializer = CreateMaterializer();
        using var firstArchive = CreateSeed(("resources/logos/php.svg", "svg"));
        materializer.EnsureInitialized(firstArchive);
        var filePath = Path.Combine(_testRoot, "resources", "logos", "php.svg");
        var firstWrite = File.GetLastWriteTimeUtc(filePath);

        using var secondArchive = CreateSeed(("resources/logos/php.svg", "svg"));
        materializer.EnsureInitialized(secondArchive);

        Assert.Equal(firstWrite, File.GetLastWriteTimeUtc(filePath));
        Assert.Equal("svg", File.ReadAllText(filePath));
    }

    [Fact]
    public void EnsureInitialized_rejects_path_traversal()
    {
        Directory.CreateDirectory(_testRoot);
        using var archive = CreateRawSeed(
            new[] { new SeedFile("../outside.txt", 4, Hash("nope")) },
            ("../outside.txt", "nope"));

        Assert.Throws<InvalidDataException>(() => CreateMaterializer().EnsureInitialized(archive));
        Assert.False(File.Exists(Path.Combine(Path.GetDirectoryName(_testRoot)!, "outside.txt")));
    }

    [Fact]
    public void EnsureInitialized_rejects_hash_mismatch_without_installing_file()
    {
        Directory.CreateDirectory(_testRoot);
        using var archive = CreateRawSeed(
            new[] { new SeedFile("catalog/modules.json", 7, Hash("expected")) },
            ("catalog/modules.json", "changed"));

        Assert.Throws<InvalidDataException>(() => CreateMaterializer().EnsureInitialized(archive));
        Assert.False(File.Exists(Path.Combine(_testRoot, "catalog", "modules.json")));
    }

    [Fact]
    public void EnsureInitialized_rejects_unexpected_archive_entry()
    {
        Directory.CreateDirectory(_testRoot);
        using var archive = CreateRawSeed(
            new[] { new SeedFile("catalog/modules.json", 7, Hash("modules")) },
            ("catalog/modules.json", "modules"),
            ("unexpected.txt", "surprise"));

        Assert.Throws<InvalidDataException>(() => CreateMaterializer().EnsureInitialized(archive));
    }

    private PortableSeedMaterializer CreateMaterializer() =>
        new(new PortablePathResolver(_testRoot));

    private static MemoryStream CreateSeed(params (string Path, string Content)[] files)
    {
        var manifestFiles = files.Select(file =>
        {
            var bytes = Encoding.UTF8.GetBytes(file.Content);
            return new SeedFile(file.Path, bytes.LongLength, Convert.ToHexStringLower(SHA256.HashData(bytes)));
        }).ToArray();
        return CreateRawSeed(manifestFiles, files);
    }

    private static MemoryStream CreateRawSeed(
        IReadOnlyList<SeedFile> manifestFiles,
        params (string Path, string Content)[] files)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var file in files)
            {
                var entry = archive.CreateEntry(file.Path);
                using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false), bufferSize: 1024, leaveOpen: false);
                writer.Write(file.Content);
            }

            var manifestEntry = archive.CreateEntry(PortableSeedMaterializer.ManifestEntryName);
            using var manifestWriter = new StreamWriter(manifestEntry.Open(), new UTF8Encoding(false), bufferSize: 1024, leaveOpen: false);
            manifestWriter.Write(JsonSerializer.Serialize(new SeedManifest(1, "test", manifestFiles)));
        }

        stream.Position = 0;
        return stream;
    }

    private static string Hash(string content) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(content)));

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    private sealed record SeedManifest(int SchemaVersion, string Version, IReadOnlyList<SeedFile> Files);

    private sealed record SeedFile(string Path, long Length, string Sha256);
}
