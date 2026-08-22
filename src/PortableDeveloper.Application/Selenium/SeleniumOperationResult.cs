namespace PortableDeveloper.Application.Selenium;

public sealed record SeleniumOperationResult(bool IsSuccess, string Detail)
{
    public static SeleniumOperationResult Success() => new(true, string.Empty);

    public static SeleniumOperationResult Failure(string detail) => new(false, detail);
}
