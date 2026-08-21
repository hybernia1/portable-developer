namespace PortableDeveloper.Application.ApachePhp;

public interface IApacheRuntimePreflight
{
    ApacheRuntimeReadiness Check(string apacheModuleRootRelativePath);
}
