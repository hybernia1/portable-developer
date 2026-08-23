using System.Security.Cryptography;

namespace PortableDeveloper.Infrastructure.Security;

/// <summary>
/// Avoids hashing an unchanged installed binary repeatedly during one application run.
/// The cache is intentionally memory-only, so every new application process performs
/// a complete integrity check again.
/// </summary>
internal sealed class FileSha256VerificationCache
{
    private readonly Dictionary<string, VerifiedFile> _verifiedFiles = new(StringComparer.OrdinalIgnoreCase);
    private readonly Func<string, string> _computeHash;
    private readonly Lock _sync = new();

    public FileSha256VerificationCache(Func<string, string>? computeHash = null)
    {
        _computeHash = computeHash ?? ComputeSha256;
    }

    public bool Matches(string path, string expectedSha256)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedSha256);

        var fullPath = Path.GetFullPath(path);
        lock (_sync)
        {
            var file = new FileInfo(fullPath);
            if (!file.Exists)
            {
                _verifiedFiles.Remove(fullPath);
                return false;
            }

            var fingerprint = new FileFingerprint(
                file.Length,
                file.CreationTimeUtc.Ticks,
                file.LastWriteTimeUtc.Ticks,
                expectedSha256);
            if (_verifiedFiles.TryGetValue(fullPath, out var verified)
                && verified.Fingerprint == fingerprint)
            {
                return true;
            }

            var matches = string.Equals(_computeHash(fullPath), expectedSha256, StringComparison.OrdinalIgnoreCase);
            if (matches)
            {
                _verifiedFiles[fullPath] = new(fingerprint);
            }
            else
            {
                _verifiedFiles.Remove(fullPath);
            }

            return matches;
        }
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private sealed record VerifiedFile(FileFingerprint Fingerprint);

    private sealed record FileFingerprint(
        long Length,
        long CreationTimeUtcTicks,
        long LastWriteTimeUtcTicks,
        string ExpectedSha256);
}
