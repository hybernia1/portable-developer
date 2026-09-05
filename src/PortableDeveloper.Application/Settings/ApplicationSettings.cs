namespace PortableDeveloper.Application.Settings;

public enum FileEditorPreference
{
    PortableWhenAvailable,
    WindowsDefault
}

public sealed record ApplicationSettings(
    ApplicationLanguage Language = ApplicationLanguage.Czech,
    FileEditorPreference EditorPreference = FileEditorPreference.PortableWhenAvailable,
    bool SeleniumFirewallNoticeAcknowledged = false)
{
    public static ApplicationSettings Default { get; } = new();
}
