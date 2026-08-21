namespace PortableDeveloper.Application.Php;

public interface IPhpRuntimePreflight
{
    PhpRuntimeReadiness Check(string phpModuleRootRelativePath);
}
