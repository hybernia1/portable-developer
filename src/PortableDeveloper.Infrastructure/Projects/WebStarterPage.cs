using System.Net;
using System.Text;

namespace PortableDeveloper.Infrastructure.Projects;

internal static class WebStarterPage
{
    public const string FileName = "index.html";

    public static bool EnsureCreated(string documentRoot, string projectName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(projectName);

        Directory.CreateDirectory(documentRoot);
        var indexPath = Path.Combine(documentRoot, FileName);
        FileStream stream;
        try
        {
            stream = new FileStream(indexPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        }
        catch (IOException) when (File.Exists(indexPath))
        {
            return false;
        }

        try
        {
            using (stream)
            {
                using var writer = new StreamWriter(stream, new UTF8Encoding(false));
                writer.Write(CreateContent(projectName));
            }

            return true;
        }
        catch
        {
            stream.Dispose();
            File.Delete(indexPath);
            throw;
        }
    }

    private static string CreateContent(string projectName) =>
        """
        <!doctype html>
        <html lang="en">
        <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <title>{{PROJECT_NAME}}</title>
            <style>
                :root { color-scheme: dark; font-family: system-ui, sans-serif; }
                body { margin: 0; min-height: 100vh; display: grid; place-items: center; background: #11151c; color: #e6edf3; }
                main { max-width: 42rem; padding: 2rem; border: 1px solid #30363d; border-radius: .75rem; background: #161b22; }
                code { color: #79c0ff; }
            </style>
        </head>
        <body>
            <main>
                <h1>Portable Developer is ready</h1>
                <p><strong>{{PROJECT_NAME}}</strong> is served from its web root.</p>
                <p>Replace <code>index.html</code> with your application entry point.</p>
            </main>
        </body>
        </html>
        """.Replace("{{PROJECT_NAME}}", WebUtility.HtmlEncode(projectName), StringComparison.Ordinal);
}
