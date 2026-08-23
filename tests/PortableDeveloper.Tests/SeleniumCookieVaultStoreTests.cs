using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Infrastructure.Paths;
using PortableDeveloper.Infrastructure.Selenium;

namespace PortableDeveloper.Tests;

public sealed class SeleniumCookieVaultStoreTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public void Import_encrypts_normalized_cookies_with_automatic_portable_key()
    {
        const string secret = "secret-session-value";
        var json = $$"""
            [
              {
                "name": "session",
                "value": "{{secret}}",
                "domain": ".Example.COM",
                "path": "/",
                "expires": 2000000000.9,
                "httpOnly": true,
                "secure": true,
                "sameSite": "no_restriction",
                "storeId": "0",
                "id": 42,
                "hostOnly": false
              },
              {
                "name": "expired",
                "value": "discard-me",
                "domain": "example.com",
                "path": "/",
                "expires": 1
              }
            ]
            """;
        var store = CreateStore();

        var imported = store.ImportJson("Example login", Encoding.UTF8.GetBytes(json));

        Assert.True(imported.IsSuccess, imported.Detail);
        Assert.Equal(1, imported.SkippedCookies);
        var vault = Assert.Single(store.GetVaults());
        Assert.False(vault.IsDamaged);
        Assert.Equal(1, vault.CookieCount);
        Assert.Equal(["example.com"], vault.Domains);
        var encryptedPath = VaultPath(vault.Id);
        var encrypted = File.ReadAllText(encryptedPath);
        Assert.DoesNotContain(secret, encrypted, StringComparison.Ordinal);
        Assert.DoesNotContain("storeId", encrypted, StringComparison.Ordinal);
        Assert.True(File.Exists(KeyPath));
        Assert.False(Directory.Exists(Path.Combine(_testRoot, "temp", "selenium-cookie-vaults")));

        var payload = DecryptVault(encryptedPath);
        Assert.Contains(secret, payload, StringComparison.Ordinal);
        Assert.Contains("\"expiry\":2000000000", payload, StringComparison.Ordinal);
        Assert.Contains("\"sameSite\":\"None\"", payload, StringComparison.Ordinal);
        Assert.DoesNotContain("storeId", payload, StringComparison.Ordinal);
        Assert.DoesNotContain("hostOnly", payload, StringComparison.Ordinal);
    }

    [Fact]
    public void GetVaults_marks_malformed_ciphertext_as_damaged_without_decrypting_it()
    {
        var store = CreateStore();
        var imported = store.ImportJson(
            "Private",
            Encoding.UTF8.GetBytes("[{\"name\":\"sid\",\"value\":\"secret\",\"domain\":\"example.com\"}]"));
        var path = VaultPath(imported.Vault!.Id);
        File.SetAttributes(path, File.GetAttributes(path) & ~FileAttributes.ReadOnly);
        var envelope = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        envelope["ciphertext"] = "not-base64";
        File.WriteAllText(path, envelope.ToJsonString());

        var vault = Assert.Single(store.GetVaults());

        Assert.True(vault.IsDamaged);
        Assert.NotEmpty(vault.Detail);
    }

    [Fact]
    public void Import_accepts_object_wrapper_and_deduplicates_last_cookie()
    {
        var store = CreateStore();
        var json = Encoding.UTF8.GetBytes("""
            {
              "cookies": [
                { "name": "sid", "value": "old", "domain": "example.com", "path": "/" },
                { "name": "sid", "value": "new", "domain": "EXAMPLE.COM", "path": "/", "unused": true }
              ]
            }
            """);

        var result = store.ImportJson("Wrapped", json);

        Assert.True(result.IsSuccess, result.Detail);
        Assert.Equal(1, result.SkippedCookies);
        Assert.Equal(1, result.Vault!.CookieCount);
        var payload = DecryptVault(VaultPath(result.Vault.Id));
        Assert.Contains("\"value\":\"new\"", payload, StringComparison.Ordinal);
        Assert.DoesNotContain("\"value\":\"old\"", payload, StringComparison.Ordinal);
        Assert.DoesNotContain("unused", payload, StringComparison.Ordinal);
    }

    [Fact]
    public void Multiple_vaults_reuse_key_without_creating_plaintext_runtime_files()
    {
        var store = CreateStore();
        var first = store.ImportJson(
            "First",
            Encoding.UTF8.GetBytes("[{\"name\":\"a\",\"value\":\"first-secret\",\"domain\":\"example.com\"}]"));
        var firstKey = File.ReadAllText(KeyPath);
        var second = store.ImportJson(
            "Second",
            Encoding.UTF8.GetBytes("[{\"name\":\"b\",\"value\":\"second-secret\",\"domain\":\"example.org\"}]"));

        Assert.True(first.IsSuccess, first.Detail);
        Assert.True(second.IsSuccess, second.Detail);
        Assert.Equal(firstKey, File.ReadAllText(KeyPath));
        Assert.DoesNotContain("first-secret", firstKey, StringComparison.Ordinal);
        Assert.DoesNotContain("second-secret", firstKey, StringComparison.Ordinal);
        Assert.False(Directory.Exists(Path.Combine(_testRoot, "temp", "selenium-cookie-vaults")));
    }

    [Fact]
    public void Missing_portable_key_marks_vault_as_damaged()
    {
        var store = CreateStore();
        var imported = store.ImportJson(
            "Missing key",
            Encoding.UTF8.GetBytes("[{\"name\":\"sid\",\"value\":\"secret\",\"domain\":\"example.com\"}]"));
        Assert.True(imported.IsSuccess, imported.Detail);
        File.Delete(KeyPath);

        var vault = Assert.Single(store.GetVaults());

        Assert.True(vault.IsDamaged);
        Assert.Contains("key", vault.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Remove_deletes_read_only_vault_file_and_its_directory()
    {
        var store = CreateStore();
        var imported = store.ImportJson(
            "Disposable",
            Encoding.UTF8.GetBytes("[{\"name\":\"sid\",\"value\":\"secret\",\"domain\":\"example.com\"}]"));
        Assert.True(imported.IsSuccess, imported.Detail);
        var vault = imported.Vault!;
        var vaultFile = VaultPath(vault.Id);
        var vaultDirectory = Path.GetDirectoryName(vaultFile)!;
        Assert.True(File.GetAttributes(vaultFile).HasFlag(FileAttributes.ReadOnly));

        var removed = store.Remove(vault.Id);

        Assert.True(removed.IsSuccess, removed.Detail);
        Assert.False(Directory.Exists(vaultDirectory));
        Assert.Empty(store.GetVaults());
    }

    [Fact]
    public void Remove_rejects_unexpected_directory_without_partially_deleting_vault()
    {
        var store = CreateStore();
        var imported = store.ImportJson(
            "Protected",
            Encoding.UTF8.GetBytes("[{\"name\":\"sid\",\"value\":\"secret\",\"domain\":\"example.com\"}]"));
        Assert.True(imported.IsSuccess, imported.Detail);
        var vaultFile = VaultPath(imported.Vault!.Id);
        Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(vaultFile)!, "unexpected"));

        var removed = store.Remove(imported.Vault.Id);

        Assert.False(removed.IsSuccess);
        Assert.True(File.Exists(vaultFile));
    }

    private string KeyPath => Path.Combine(_testRoot, "state", "selenium-cookie-vault.key");

    private string VaultPath(string id) =>
        Path.Combine(_testRoot, "profiles", "selenium-vaults", id, "vault.json");

    private string DecryptVault(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        var key = Convert.FromBase64String(File.ReadAllText(KeyPath));
        var associatedData = Convert.FromBase64String(root.GetProperty("associatedData").GetString()!);
        var nonce = Convert.FromBase64String(root.GetProperty("nonce").GetString()!);
        var tag = Convert.FromBase64String(root.GetProperty("tag").GetString()!);
        var ciphertext = Convert.FromBase64String(root.GetProperty("ciphertext").GetString()!);
        var plaintext = new byte[ciphertext.Length];
        try
        {
            using var aes = new AesGcm(key, tag.Length);
            aes.Decrypt(nonce, ciphertext, tag, plaintext, associatedData);
            return Encoding.UTF8.GetString(plaintext);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    private SeleniumCookieVaultStore CreateStore() =>
        new(new PortablePathResolver(_testRoot), new SilentLogger());

    public void Dispose()
    {
        if (!Directory.Exists(_testRoot))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(_testRoot, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, File.GetAttributes(file) & ~FileAttributes.ReadOnly);
        }

        Directory.Delete(_testRoot, recursive: true);
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
