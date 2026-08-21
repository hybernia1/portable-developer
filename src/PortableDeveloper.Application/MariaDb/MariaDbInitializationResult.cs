namespace PortableDeveloper.Application.MariaDb;

public sealed record MariaDbInitializationResult(
    MariaDbInitializationStatus Status,
    string Detail);
