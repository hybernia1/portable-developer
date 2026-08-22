using System.Text;
using System.Text.Json;
using PortableDeveloper.Application.Abstractions;

namespace PortableDeveloper.Infrastructure.MariaDb;

internal sealed class MariaDbCredentialStore(IPortablePathResolver paths)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public MariaDbStoredCredentials Read(string instanceId)
    {
        var path = paths.Resolve(Path.Combine("instances", instanceId, "state", "mariadb-credentials.json"));
        var credentials = JsonSerializer.Deserialize<MariaDbStoredCredentials>(File.ReadAllText(path), SerializerOptions);
        return credentials ?? throw new InvalidDataException("MariaDB credentials are not readable.");
    }

    public void Write(string instanceId, MariaDbStoredCredentials credentials)
    {
        var relativeDirectory = Path.Combine("instances", instanceId, "state");
        paths.EnsureDirectory(relativeDirectory);
        var targetPath = paths.Resolve(Path.Combine(relativeDirectory, "mariadb-credentials.json"));
        var stagingPath = targetPath + $".{Guid.NewGuid():N}.tmp";
        try
        {
            WriteToPath(stagingPath, credentials);
            File.Move(stagingPath, targetPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(stagingPath))
            {
                File.Delete(stagingPath);
            }
        }
    }

    public void WriteToPath(string path, MariaDbStoredCredentials credentials) =>
        File.WriteAllText(
            path,
            JsonSerializer.Serialize(credentials, SerializerOptions),
            new UTF8Encoding(false));
}

internal sealed record MariaDbStoredCredentials(
    string UserName,
    string Password,
    int Port,
    DateTimeOffset CreatedAtUtc);
