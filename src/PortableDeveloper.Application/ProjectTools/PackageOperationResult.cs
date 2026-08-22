namespace PortableDeveloper.Application.ProjectTools;

public sealed record PackageOperationResult(bool IsSuccess, string Detail)
{
    public static PackageOperationResult Success(string detail) => new(true, detail);

    public static PackageOperationResult Failure(string detail) => new(false, detail);
}
