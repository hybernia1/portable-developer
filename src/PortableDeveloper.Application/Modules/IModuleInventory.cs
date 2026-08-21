using PortableDeveloper.Domain.Modules;

namespace PortableDeveloper.Application.Modules;

public interface IModuleInventory
{
    IReadOnlyList<ModuleInstallation> GetInstalled(ModuleKind kind);
}
