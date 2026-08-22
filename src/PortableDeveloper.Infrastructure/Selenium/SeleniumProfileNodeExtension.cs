using System.Security.Cryptography;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Selenium;
using PortableDeveloper.Domain.Processes;

namespace PortableDeveloper.Infrastructure.Selenium;

public sealed class SeleniumProfileNodeExtension : ISeleniumProfileNodeExtension
{
    private const string SourceRelativePath = "resources/selenium/PortableProfileNode.java";
    private const string OutputRoot = "temp/generated/default/selenium/profile-node";
    private readonly IPortablePathResolver _paths;
    private readonly IPortableCommandRunner _runner;

    public SeleniumProfileNodeExtension(IPortablePathResolver paths, IPortableCommandRunner runner)
    {
        _paths = paths;
        _runner = runner;
    }

    public async Task<string> EnsureBuiltAsync(
        string javaRuntimeRelativePath,
        string seleniumJarRelativePath,
        CancellationToken cancellationToken = default)
    {
        var source = _paths.Resolve(SourceRelativePath);
        var seleniumJar = _paths.Resolve(seleniumJarRelativePath);
        if (!File.Exists(source))
        {
            throw new FileNotFoundException("The Selenium profile-node source is missing from the application package.", source);
        }

        var binRelativePath = Path.Combine(javaRuntimeRelativePath, "bin");
        var javacRelativePath = Path.Combine(binRelativePath, "javac.exe");
        var jarToolRelativePath = Path.Combine(binRelativePath, "jar.exe");
        if (!File.Exists(_paths.Resolve(javacRelativePath)) || !File.Exists(_paths.Resolve(jarToolRelativePath)))
        {
            throw new FileNotFoundException("The portable Java runtime does not contain the compiler tools required by the profile-node extension.");
        }

        var output = _paths.EnsureDirectory(OutputRoot);
        var classesRelativePath = Path.Combine(OutputRoot, "classes");
        var classes = _paths.Resolve(classesRelativePath);
        var jarRelativePath = Path.Combine(OutputRoot, "portable-profile-node.jar");
        var jar = _paths.Resolve(jarRelativePath);
        var marker = Path.Combine(output, "build.sha256");
        var expectedBuildHash = ComputeCombinedHash(source, seleniumJar);
        if (File.Exists(jar)
            && File.Exists(marker)
            && string.Equals(File.ReadAllText(marker).Trim(), expectedBuildHash, StringComparison.OrdinalIgnoreCase))
        {
            return jarRelativePath;
        }

        if (Directory.Exists(classes))
        {
            Directory.Delete(classes, recursive: true);
        }

        Directory.CreateDirectory(classes);
        if (File.Exists(jar))
        {
            File.Delete(jar);
        }

        var compile = await _runner.RunAsync(
            new PortableCommandDefinition(
                "selenium-profile-node-compile",
                javacRelativePath,
                OutputRoot,
                ["-encoding", "UTF-8", "-classpath", seleniumJar, "-d", classes, source],
                Timeout: TimeSpan.FromMinutes(1)),
            cancellationToken);
        if (!compile.IsSuccess)
        {
            throw new InvalidDataException($"The Selenium profile-node extension could not be compiled: {compile.StandardError}");
        }

        var package = await _runner.RunAsync(
            new PortableCommandDefinition(
                "selenium-profile-node-package",
                jarToolRelativePath,
                OutputRoot,
                ["--create", "--file", jar, "-C", classes, "."],
                Timeout: TimeSpan.FromMinutes(1)),
            cancellationToken);
        if (!package.IsSuccess || !File.Exists(jar))
        {
            throw new InvalidDataException($"The Selenium profile-node extension could not be packaged: {package.StandardError}");
        }

        File.WriteAllText(marker, expectedBuildHash);
        return jarRelativePath;
    }

    private static string ComputeCombinedHash(string source, string seleniumJar)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var path in new[] { source, seleniumJar })
        {
            using var stream = File.OpenRead(path);
            var buffer = new byte[81920];
            int read;
            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                hash.AppendData(buffer, 0, read);
            }
        }

        return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
    }
}
