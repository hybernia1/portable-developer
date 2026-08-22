using PortableDeveloper.Domain.Packages;

namespace PortableDeveloper.Application.Packages;

public interface IDependencyLockCatalog
{
    DependencyLockCatalog Load();
}
