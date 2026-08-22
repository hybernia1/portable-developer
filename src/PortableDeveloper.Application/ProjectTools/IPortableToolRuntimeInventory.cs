namespace PortableDeveloper.Application.ProjectTools;

public interface IPortableToolRuntimeInventory
{
    PortableToolRuntimeInfo GetRuntime(PortableToolKind kind);
}
