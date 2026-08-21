namespace PortableDeveloper.Application.MariaDb;

public sealed record MariaDbInstanceOptions(
    string InstanceId = "default",
    int Port = 3307);
