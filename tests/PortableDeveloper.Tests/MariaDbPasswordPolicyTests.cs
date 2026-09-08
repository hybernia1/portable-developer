using PortableDeveloper.Application.MariaDb;

namespace PortableDeveloper.Tests;

public sealed class MariaDbPasswordPolicyTests
{
    [Theory]
    [InlineData("1234567", false)]
    [InlineData("12345678", true)]
    [InlineData("valid-password", true)]
    [InlineData("contains\0null", false)]
    public void Validation_matches_the_portable_account_boundary(string password, bool expected)
    {
        Assert.Equal(expected, MariaDbPasswordPolicy.IsValid(password));
    }

    [Fact]
    public void Passwords_longer_than_the_limit_are_rejected()
    {
        Assert.False(MariaDbPasswordPolicy.IsValid(new string('x', MariaDbPasswordPolicy.MaximumLength + 1)));
    }
}
