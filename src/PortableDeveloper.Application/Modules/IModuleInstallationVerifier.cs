using PortableDeveloper.Domain.Modules;

namespace PortableDeveloper.Application.Modules;

public interface IModuleInstallationVerifier
{
    ModuleInstallationVerification Verify(ModuleKind kind, string displayName);
}
