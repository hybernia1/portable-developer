namespace PortableDeveloper.Application.MariaDb;

public static class MariaDbPasswordPolicy
{
    public const int MinimumLength = 8;

    public const int MaximumLength = 128;

    public static bool IsValid(string password) =>
        password.Length is >= MinimumLength and <= MaximumLength
        && !password.Contains('\0');
}
