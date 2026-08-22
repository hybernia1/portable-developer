namespace PortableDeveloper.Application.Selenium;

public interface ISeleniumProfileStore
{
    IReadOnlyList<SeleniumProfileInfo> GetProfiles();

    SeleniumProfileOperationResult Import(
        string name,
        SeleniumProfileBrowser browser,
        string sourceDirectory);

    SeleniumProfileOperationResult Remove(string id);

    string CreateSessionCopy(string profileId, string sessionToken);

    void DeleteSessionCopy(string sessionToken);

    void DeleteAllSessionCopies();
}
