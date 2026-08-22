namespace PortableDeveloper.Application.Selenium;

public interface ISeleniumProfileNodeExtension
{
    Task<string> EnsureBuiltAsync(
        string javaRuntimeRelativePath,
        string seleniumJarRelativePath,
        CancellationToken cancellationToken = default);
}
