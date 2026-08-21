using PortableDeveloper.Domain.Modules;

namespace PortableDeveloper.Application.Modules;

public sealed record ModuleInstallationVerification(ModuleInstallation? Installation, string Detail)
{
    public bool IsVerified => Installation is not null;
}
