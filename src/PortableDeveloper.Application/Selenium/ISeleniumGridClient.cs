namespace PortableDeveloper.Application.Selenium;

public interface ISeleniumGridClient
{
    Task<bool> IsReadyAsync(int port, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SeleniumSessionInfo>> ListSessionsAsync(
        int port,
        CancellationToken cancellationToken = default);

    Task<SeleniumOperationResult> TerminateSessionAsync(
        int port,
        string sessionId,
        CancellationToken cancellationToken = default);
}
