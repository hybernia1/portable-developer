namespace PortableDeveloper.Application.Settings;

public sealed record ApplicationSettings(ApplicationLanguage Language)
{
    public static ApplicationSettings Default { get; } = new(ApplicationLanguage.Czech);
}
