using PortableDeveloper.Domain.Packages;

namespace PortableDeveloper.Application.Packages;

public interface IModulePackageCatalog
{
    ModulePackageCatalog Load();
}
