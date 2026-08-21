namespace PortableDeveloper.Application.MariaDb;

public sealed record DatabaseOperationResult(bool IsSuccess, string Detail)
{
    public static DatabaseOperationResult Success(string detail = "") => new(true, detail);

    public static DatabaseOperationResult Failure(string detail) => new(false, detail);
}
