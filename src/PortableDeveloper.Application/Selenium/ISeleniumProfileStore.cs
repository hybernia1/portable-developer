namespace PortableDeveloper.Application.Selenium;

public interface ISeleniumProfileStore
{
    IReadOnlyList<SeleniumProfileInfo> GetProfiles();

    SeleniumProfileOperationResult CreateFromManagedDraft(
        string name,
        SeleniumProfileBrowser browser,
        string draftRelativePath,
        string? browserVersion = null);

    SeleniumProfileOperationResult Remove(string id);

    string CreateSessionCopy(string profileId, string sessionToken);

    void DeleteSessionCopy(string sessionToken);

    void DeleteAllSessionCopies();
}
