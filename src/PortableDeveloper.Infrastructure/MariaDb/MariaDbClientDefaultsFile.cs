using System.Text;
using PortableDeveloper.Application.Abstractions;

namespace PortableDeveloper.Infrastructure.MariaDb;

internal sealed class MariaDbClientDefaultsFile : IDisposable
{
    public MariaDbClientDefaultsFile(
        IPortablePathResolver paths,
        MariaDbStoredCredentials credentials,
        int port)
    {
        var relativeDirectory = Path.Combine("temp", "mariadb-client");
        paths.EnsureDirectory(relativeDirectory);
        FilePath = paths.Resolve(System.IO.Path.Combine(relativeDirectory, $"client-{Guid.NewGuid():N}.ini"));
        File.WriteAllText(
            FilePath,
            $"""
            [client]
            protocol=tcp
            host=127.0.0.1
            port={port}
            user={Escape(credentials.UserName)}
            password="{Escape(credentials.Password)}"
            """,
            new UTF8Encoding(false));
    }

    public string FilePath { get; }

    public string Argument => $"--defaults-extra-file={FilePath}";

    public void Dispose()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }
        }
        catch (IOException)
        {
            // A stale temporary credential file stays inside the portable root and is cleaned on the next launch.
        }
        catch (UnauthorizedAccessException)
        {
            // Cleanup must not hide the database command result.
        }
    }

    private static string Escape(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("\"", "\\\"", StringComparison.Ordinal)
        .Replace("\r", "\\r", StringComparison.Ordinal)
        .Replace("\n", "\\n", StringComparison.Ordinal)
        .Replace("\t", "\\t", StringComparison.Ordinal);
}
