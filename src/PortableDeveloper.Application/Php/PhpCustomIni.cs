namespace PortableDeveloper.Application.Php;

public static class PhpCustomIni
{
    public const int MaximumSizeBytes = 256 * 1024;

    public const string InitialContent =
        "; Portable Developer - custom PHP settings\r\n" +
        "; This file is appended to the generated php.ini on every web stack start.\r\n" +
        "; Prefer the validated application form for common settings.\r\n" +
        "; Manual changes are advanced and take effect after restarting the web stack.\r\n";

    public static string GetRelativePath(string instanceId) =>
        Path.Combine("instances", instanceId, "config", "php-custom.ini");
}
