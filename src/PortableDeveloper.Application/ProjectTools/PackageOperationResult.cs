namespace PortableDeveloper.Application.ProjectTools;

public enum PackageOperationOutcome
{
    None,
    Installed,
    PromotedToDirect,
    AlreadyDirect,
    Removed
}

public sealed record PackageOperationResult(
    bool IsSuccess,
    string Detail,
    PackageOperationOutcome Outcome = PackageOperationOutcome.None)
{
    public static PackageOperationResult Success(
        string detail,
        PackageOperationOutcome outcome = PackageOperationOutcome.None) => new(true, detail, outcome);

    public static PackageOperationResult Failure(string detail) => new(false, detail);
}
