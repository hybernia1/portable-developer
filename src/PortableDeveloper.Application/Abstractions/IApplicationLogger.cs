namespace PortableDeveloper.Application.Abstractions;

public interface IApplicationLogger
{
    ValueTask LogAsync(
        ApplicationLogLevel level,
        string component,
        string eventName,
        string message,
        CancellationToken cancellationToken = default);
}
