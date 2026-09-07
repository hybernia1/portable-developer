using PortableDeveloper.App.Shell;

namespace PortableDeveloper.Tests;

public sealed class WorkspaceLayoutPolicyTests
{
    [Theory]
    [InlineData(0, WorkspaceLayoutMode.Compact)]
    [InlineData(759.99, WorkspaceLayoutMode.Compact)]
    [InlineData(760, WorkspaceLayoutMode.Wide)]
    [InlineData(1600, WorkspaceLayoutMode.Wide)]
    [InlineData(double.PositiveInfinity, WorkspaceLayoutMode.Wide)]
    public void Available_width_resolves_to_one_shared_layout_mode(
        double availableWidth,
        WorkspaceLayoutMode expected)
    {
        Assert.Equal(expected, WorkspaceLayoutPolicy.GetMode(availableWidth));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(double.NaN)]
    public void Invalid_available_width_is_rejected(double availableWidth)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => WorkspaceLayoutPolicy.GetMode(availableWidth));
    }
}
