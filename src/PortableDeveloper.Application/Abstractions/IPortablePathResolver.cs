namespace PortableDeveloper.Application.Abstractions;

public interface IPortablePathResolver
{
    string RootPath { get; }

    string Resolve(string relativePath);

    string EnsureDirectory(string relativePath);
}
