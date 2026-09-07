namespace PortableDeveloper.App.Shell;

public enum WorkspaceLayoutMode
{
    Compact,
    Wide
}

public static class WorkspaceLayoutPolicy
{
    public const double WideMinimumWidth = 760d;

    public static WorkspaceLayoutMode GetMode(double availableWidth)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(availableWidth);
        if (double.IsNaN(availableWidth))
        {
            throw new ArgumentOutOfRangeException(nameof(availableWidth));
        }

        return availableWidth >= WideMinimumWidth
            ? WorkspaceLayoutMode.Wide
            : WorkspaceLayoutMode.Compact;
    }
}
