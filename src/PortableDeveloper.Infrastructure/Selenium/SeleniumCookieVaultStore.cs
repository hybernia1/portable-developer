using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Selenium;

namespace PortableDeveloper.Infrastructure.Selenium;

public sealed partial class SeleniumCookieVaultStore : ISeleniumCookieVaultStore
{
    private const string VaultsRoot = "profiles/selenium-vaults";
    private const string MasterKeyRelativePath = "state/selenium-cookie-vault.key";
    private const string StagingRoot = "temp/cookie-vault-import";
    private const int SchemaVersion = 2;
    private const int MaximumImportBytes = 5 * 1024 * 1024;
    private const int MaximumCookies = 5_000;
    private const int MaximumCookieNameCharacters = 256;
    private const int MaximumCookieValueCharacters = 16_384;
    private const int MaximumCookiePathCharacters = 2_048;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    private static readonly JsonSerializerOptions PayloadOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    private static readonly SearchValues<char> CookieNameSeparators = SearchValues.Create("()<>@,;:\\\"/[]?={} \t");

    private readonly IPortablePathResolver _paths;
    private readonly IApplicationLogger _logger;

    public SeleniumCookieVaultStore(IPortablePathResolver paths, IApplicationLogger logger)
    {
        _paths = paths;
        _logger = logger;
    }

    public IReadOnlyList<SeleniumCookieVaultInfo> GetVaults()
    {
        var root = _paths.EnsureDirectory(VaultsRoot);
        return Directory.EnumerateDirectories(root, "*", SearchOption.TopDirectoryOnly)
            .Where(path => !IsReparsePoint(path))
            .Select(ReadVault)
            .Where(vault => vault is not null)
            .Cast<SeleniumCookieVaultInfo>()
            .OrderBy(vault => vault.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    public SeleniumCookieVaultOperationResult ImportJson(
        string name,
        byte[] json)
    {
        byte[]? plaintext = null;
        byte[]? key = null;
        try
        {
            var normalizedName = ValidateName(name);
            if (json.Length == 0 || json.Length > MaximumImportBytes)
            {
                return SeleniumCookieVaultOperationResult.Failure("The cookie export must be between 1 byte and 5 MiB.");
            }

            var normalized = NormalizeCookies(json);
            if (normalized.Cookies.Count == 0)
            {
                return SeleniumCookieVaultOperationResult.Failure("The export contains no valid, unexpired cookies.");
            }

            var id = Guid.NewGuid().ToString("N");
            var importedAt = DateTimeOffset.UtcNow;
            var domains = normalized.Cookies
                .Select(cookie => cookie.Domain.TrimStart('.'))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(domain => domain, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            plaintext = JsonSerializer.SerializeToUtf8Bytes(normalized.Cookies, PayloadOptions);
            key = GetOrCreateMasterKey();
            var nonce = RandomNumberGenerator.GetBytes(12);
            var tag = new byte[16];
            var ciphertext = new byte[plaintext.Length];
            var associatedData = BuildAssociatedData(id, normalizedName, normalized.Cookies.Count, domains, importedAt);
            using (var aes = new AesGcm(key, tag.Length))
            {
                aes.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);
            }

            var envelope = new VaultEnvelope(
                SchemaVersion,
                id,
                normalizedName,
                normalized.Cookies.Count,
                domains,
                importedAt,
                Convert.ToBase64String(associatedData),
                Convert.ToBase64String(nonce),
                Convert.ToBase64String(tag),
                Convert.ToBase64String(ciphertext));
            SaveEnvelopeTransactionally(envelope);
            var vault = ReadVault(_paths.Resolve(Path.Combine(VaultsRoot, id)))
                ?? throw new InvalidDataException("The imported cookie vault could not be read back.");
            if (vault.IsDamaged)
            {
                throw new InvalidDataException(vault.Detail);
            }
            Log("selenium.cookie-vault.imported", $"vault={id}; cookies={vault.CookieCount}; domains={vault.Domains.Count}");
            return SeleniumCookieVaultOperationResult.Success(vault, normalized.SkippedCookies);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException
                                           or ArgumentException or JsonException or CryptographicException)
        {
            return SeleniumCookieVaultOperationResult.Failure(exception.Message);
        }
        finally
        {
            Clear(plaintext);
            Clear(key);
        }
    }

    public SeleniumCookieVaultOperationResult Remove(string id)
    {
        try
        {
            ValidateId(id);
            var path = _paths.Resolve(Path.Combine(VaultsRoot, id));
            if (!Directory.Exists(path))
            {
                return SeleniumCookieVaultOperationResult.Failure("The cookie vault does not exist.");
            }

            if (IsReparsePoint(path))
            {
                return SeleniumCookieVaultOperationResult.Failure("The cookie vault directory is unsafe.");
            }

            var files = new List<string>();
            foreach (var entry in Directory.EnumerateFileSystemEntries(path, "*", SearchOption.TopDirectoryOnly))
            {
                if (IsReparsePoint(entry))
                {
                    return SeleniumCookieVaultOperationResult.Failure("The cookie vault contains an unsafe link.");
                }

                if (!File.Exists(entry))
                {
                    return SeleniumCookieVaultOperationResult.Failure("The cookie vault contains an unexpected directory.");
                }

                files.Add(entry);
            }

            foreach (var file in files)
            {
                File.SetAttributes(file, File.GetAttributes(file) & ~FileAttributes.ReadOnly);
                File.Delete(file);
            }

            Directory.Delete(path, recursive: false);
            Log("selenium.cookie-vault.removed", $"vault={id}");
            return SeleniumCookieVaultOperationResult.Success();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return SeleniumCookieVaultOperationResult.Failure(exception.Message);
        }
    }

    private SeleniumCookieVaultInfo? ReadVault(string directory)
    {
        byte[]? key = null;
        try
        {
            var envelope = ReadEnvelope(directory);
            ValidateEnvelope(envelope, Path.GetFileName(directory));
            key = ReadMasterKey();
            return new(
                envelope.Id,
                envelope.Name,
                envelope.CookieCount,
                envelope.Domains,
                envelope.ImportedAtUtc,
                false,
                string.Empty);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException
                                           or JsonException or ArgumentException or FormatException or CryptographicException)
        {
            var id = Path.GetFileName(directory);
            return IdPattern().IsMatch(id)
                ? new(id, id, 0, [], DateTimeOffset.MinValue, true, exception.Message)
                : null;
        }
        finally
        {
            Clear(key);
        }
    }

    private VaultEnvelope ReadEnvelope(string directory)
    {
        if (!Directory.Exists(directory) || IsReparsePoint(directory))
        {
            throw new InvalidDataException("The cookie vault directory is missing or unsafe.");
        }

        var path = Path.Combine(directory, "vault.json");
        if (!File.Exists(path) || IsReparsePoint(path) || new FileInfo(path).Length > MaximumImportBytes * 2L)
        {
            throw new InvalidDataException("The encrypted cookie vault file is missing or unsafe.");
        }

        return JsonSerializer.Deserialize<VaultEnvelope>(File.ReadAllText(path), JsonOptions)
               ?? throw new InvalidDataException("The encrypted cookie vault metadata is invalid.");
    }

    private static void ValidateEnvelope(VaultEnvelope envelope, string expectedId)
    {
        if (envelope.SchemaVersion != SchemaVersion
            || string.IsNullOrWhiteSpace(envelope.Id)
            || !IdPattern().IsMatch(envelope.Id)
            || !string.Equals(envelope.Id, expectedId, StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(envelope.Name)
            || envelope.Name != ValidateName(envelope.Name)
            || envelope.CookieCount is < 1 or > MaximumCookies
            || envelope.Domains is null
            || envelope.Domains.Count is < 1 or > MaximumCookies
            || envelope.ImportedAtUtc == default
            || envelope.Domains.Any(domain => string.IsNullOrWhiteSpace(domain) || !IsValidDomain(domain))
            || string.IsNullOrWhiteSpace(envelope.AssociatedData)
            || string.IsNullOrWhiteSpace(envelope.Nonce)
            || string.IsNullOrWhiteSpace(envelope.Tag)
            || string.IsNullOrWhiteSpace(envelope.Ciphertext))
        {
            throw new InvalidDataException("The encrypted cookie vault metadata is invalid.");
        }

        var associatedData = Convert.FromBase64String(envelope.AssociatedData);
        var nonce = DecodeFixed(envelope.Nonce, 12, "nonce");
        var tag = DecodeFixed(envelope.Tag, 16, "authentication tag");
        var ciphertext = Convert.FromBase64String(envelope.Ciphertext);
        try
        {
            if (associatedData.Length is < 1 or > 16_384
                || ciphertext.Length is < 1 or > MaximumImportBytes)
            {
                throw new InvalidDataException("The encrypted cookie vault payload has an invalid size.");
            }

            var expectedAssociatedData = BuildAssociatedData(
                envelope.Id,
                envelope.Name,
                envelope.CookieCount,
                envelope.Domains,
                envelope.ImportedAtUtc);
            if (!CryptographicOperations.FixedTimeEquals(associatedData, expectedAssociatedData))
            {
                throw new InvalidDataException("The cookie vault metadata authentication data does not match.");
            }
        }
        finally
        {
            Clear(associatedData);
            Clear(nonce);
            Clear(tag);
            Clear(ciphertext);
        }
    }

    private void SaveEnvelopeTransactionally(VaultEnvelope envelope)
    {
        var staging = _paths.Resolve(Path.Combine(StagingRoot, envelope.Id));
        var target = _paths.Resolve(Path.Combine(VaultsRoot, envelope.Id));
        Directory.CreateDirectory(Path.GetDirectoryName(staging)!);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        Directory.CreateDirectory(staging);
        try
        {
            var path = Path.Combine(staging, "vault.json");
            File.WriteAllText(path, JsonSerializer.Serialize(envelope, JsonOptions), new UTF8Encoding(false));
            File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.ReadOnly);
            Directory.Move(staging, target);
        }
        finally
        {
            if (Directory.Exists(staging) && !IsReparsePoint(staging))
            {
                foreach (var file in Directory.EnumerateFiles(staging, "*", SearchOption.TopDirectoryOnly))
                {
                    File.SetAttributes(file, File.GetAttributes(file) & ~FileAttributes.ReadOnly);
                }

                Directory.Delete(staging, recursive: false);
            }
        }
    }

    private byte[] GetOrCreateMasterKey()
    {
        var path = _paths.Resolve(MasterKeyRelativePath);
        if (File.Exists(path))
        {
            return ReadMasterKey();
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var key = RandomNumberGenerator.GetBytes(32);
        var temporary = $"{path}.{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(temporary, Convert.ToBase64String(key), new UTF8Encoding(false));
            try
            {
                File.Move(temporary, path);
            }
            catch (IOException) when (File.Exists(path))
            {
                File.Delete(temporary);
                Clear(key);
                return ReadMasterKey();
            }

            return key;
        }
        catch
        {
            Clear(key);
            throw;
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }

    private byte[] ReadMasterKey()
    {
        var path = _paths.Resolve(MasterKeyRelativePath);
        if (!File.Exists(path) || IsReparsePoint(path) || new FileInfo(path).Length > 128)
        {
            throw new InvalidDataException("The portable cookie vault key is missing or unsafe.");
        }

        return DecodeFixed(File.ReadAllText(path).Trim(), 32, "master key");
    }

    private static NormalizationResult NormalizeCookies(byte[] json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (root.ValueKind == JsonValueKind.Object)
        {
            root = root.EnumerateObject()
                .FirstOrDefault(property => property.Name.Equals("cookies", StringComparison.OrdinalIgnoreCase))
                .Value;
        }

        if (root.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("The cookie export must be a JSON array or an object containing a cookies array.");
        }

        var cookies = new Dictionary<string, NormalizedCookie>(StringComparer.Ordinal);
        var skipped = 0;
        foreach (var element in root.EnumerateArray())
        {
            if (cookies.Count + skipped >= MaximumCookies)
            {
                throw new InvalidDataException("The cookie export exceeds the 5,000 cookie limit.");
            }

            if (!TryNormalizeCookie(element, out var cookie))
            {
                skipped++;
                continue;
            }

            var key = $"{cookie.Domain.ToLowerInvariant()}\u001f{cookie.Path}\u001f{cookie.Name}";
            if (cookies.ContainsKey(key))
            {
                skipped++;
            }

            cookies[key] = cookie;
        }

        return new(
            cookies.Values
                .OrderBy(cookie => cookie.Domain, StringComparer.OrdinalIgnoreCase)
                .ThenBy(cookie => cookie.Path, StringComparer.Ordinal)
                .ThenBy(cookie => cookie.Name, StringComparer.Ordinal)
                .ToArray(),
            skipped);
    }

    private static bool TryNormalizeCookie(JsonElement element, out NormalizedCookie cookie)
    {
        cookie = default!;
        if (element.ValueKind != JsonValueKind.Object
            || !TryGetString(element, "name", out var name)
            || !TryGetString(element, "value", out var value)
            || !TryGetString(element, "domain", out var domain))
        {
            return false;
        }

        domain = domain.Trim().ToLowerInvariant();
        var path = TryGetString(element, "path", out var suppliedPath) ? suppliedPath : "/";
        if (!IsValidCookieName(name)
            || value.Length > MaximumCookieValueCharacters
            || value.Contains('\0')
            || !IsValidDomain(domain)
            || string.IsNullOrEmpty(path)
            || path[0] != '/'
            || path.Length > MaximumCookiePathCharacters
            || path.Contains('\0'))
        {
            return false;
        }

        var secure = TryGetBoolean(element, "secure", out var secureValue) && secureValue;
        var httpOnly = TryGetBoolean(element, "httpOnly", out var httpOnlyValue) && httpOnlyValue;
        long? expiry = null;
        if (TryGetNumber(element, ["expires", "expiry", "expirationDate", "expiration"], out var expiryValue)
            && expiryValue > 0)
        {
            if (expiryValue > 253_402_300_799D)
            {
                return false;
            }

            expiry = checked((long)Math.Floor(expiryValue));
            if (expiry <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            {
                return false;
            }
        }

        string? sameSite = null;
        if (TryGetString(element, "sameSite", out var suppliedSameSite))
        {
            sameSite = suppliedSameSite.Trim().ToLowerInvariant() switch
            {
                "strict" => "Strict",
                "lax" => "Lax",
                "none" or "no_restriction" => "None",
                "unspecified" or "" => null,
                _ => null
            };
            if (sameSite == "None" && !secure)
            {
                sameSite = null;
            }
        }

        cookie = new(name, value, domain, path, expiry, httpOnly, secure, sameSite);
        return true;
    }

    private static bool TryGetString(JsonElement element, string name, out string value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                && property.Value.ValueKind == JsonValueKind.String)
            {
                value = property.Value.GetString() ?? string.Empty;
                return true;
            }
        }

        value = string.Empty;
        return false;
    }

    private static bool TryGetBoolean(JsonElement element, string name, out bool value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (!property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (property.Value.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                value = property.Value.GetBoolean();
                return true;
            }
        }

        value = false;
        return false;
    }

    private static bool TryGetNumber(JsonElement element, string[] names, out double value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (!names.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            if (property.Value.ValueKind == JsonValueKind.Number && property.Value.TryGetDouble(out value))
            {
                return true;
            }

            if (property.Value.ValueKind == JsonValueKind.String
                && double.TryParse(property.Value.GetString(), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out value))
            {
                return true;
            }
        }

        value = 0;
        return false;
    }

    private static bool IsValidCookieName(string name) =>
        name.Length is > 0 and <= MaximumCookieNameCharacters
        && !name.AsSpan().ContainsAny(CookieNameSeparators)
        && !name.Any(character => character is < (char)0x21 or >= (char)0x7f);

    private static bool IsValidDomain(string domain)
    {
        var host = domain.TrimStart('.');
        return domain.Length <= 254
               && host.Length is > 0 and <= 253
               && DomainPattern().IsMatch(host);
    }

    private static byte[] BuildAssociatedData(
        string id,
        string name,
        int cookieCount,
        IReadOnlyList<string> domains,
        DateTimeOffset importedAtUtc) =>
        Encoding.UTF8.GetBytes(
            $"PortableDeveloper.CookieVault.v2\n{id}\n{name}\n{cookieCount}\n{string.Join('\n', domains)}\n{importedAtUtc:O}");

    private static byte[] DecodeFixed(string value, int expectedLength, string label)
    {
        var decoded = Convert.FromBase64String(value);
        if (decoded.Length != expectedLength)
        {
            throw new InvalidDataException($"The cookie vault {label} is invalid.");
        }

        return decoded;
    }

    private static string ValidateName(string name)
    {
        var normalized = name?.Trim() ?? string.Empty;
        if (normalized.Length is < 1 or > 80 || normalized.Any(char.IsControl))
        {
            throw new ArgumentException("The cookie vault name must contain 1 to 80 printable characters.", nameof(name));
        }

        return normalized;
    }

    private static void ValidateId(string id)
    {
        if (!IdPattern().IsMatch(id))
        {
            throw new ArgumentException("The cookie vault identifier is invalid.", nameof(id));
        }
    }

    private static bool IsReparsePoint(string path) =>
        (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;

    private static void Clear(byte[]? data)
    {
        if (data is not null)
        {
            CryptographicOperations.ZeroMemory(data);
        }
    }

    private void Log(string eventName, string message) =>
        _ = _logger.LogAsync(ApplicationLogLevel.Information, "selenium-cookie-vault", eventName, message);

    [GeneratedRegex("^[a-fA-F0-9]{32}$", RegexOptions.CultureInvariant)]
    private static partial Regex IdPattern();

    [GeneratedRegex("^(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\\.)*[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DomainPattern();

    private sealed record VaultEnvelope(
        int SchemaVersion,
        string Id,
        string Name,
        int CookieCount,
        IReadOnlyList<string> Domains,
        DateTimeOffset ImportedAtUtc,
        string AssociatedData,
        string Nonce,
        string Tag,
        string Ciphertext);

    private sealed record NormalizedCookie(
        string Name,
        string Value,
        string Domain,
        string Path,
        long? Expiry,
        bool HttpOnly,
        bool Secure,
        string? SameSite);

    private sealed record NormalizationResult(
        IReadOnlyList<NormalizedCookie> Cookies,
        int SkippedCookies);
}
