namespace PortableDeveloper.Application.ApachePhp;

public interface IApachePhpConfigurationGenerator
{
    GeneratedApachePhpConfiguration Generate(ApachePhpInstanceConfiguration configuration);
}
