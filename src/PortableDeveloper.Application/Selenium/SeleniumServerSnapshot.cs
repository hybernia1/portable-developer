using PortableDeveloper.Domain.Processes;

namespace PortableDeveloper.Application.Selenium;

public sealed record SeleniumServerSnapshot(
    ManagedProcessState State,
    string Detail,
    int? ProcessId = null);
