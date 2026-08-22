namespace PortableDeveloper.Application.Ports;

public static class PortSettingsValidator
{
    public const int MinimumPort = 1024;
    public const int MaximumPort = 65535;

    public static PortSettings Validate(PortSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var assignments = new Dictionary<string, int>
        {
            [nameof(settings.ApachePort)] = settings.ApachePort,
            [nameof(settings.PhpFastCgiPort)] = settings.PhpFastCgiPort,
            [nameof(settings.MariaDbPort)] = settings.MariaDbPort,
            [nameof(settings.SeleniumPort)] = settings.SeleniumPort
        };

        foreach (var assignment in assignments)
        {
            if (assignment.Value is < MinimumPort or > MaximumPort)
            {
                throw new ArgumentOutOfRangeException(
                    assignment.Key,
                    assignment.Value,
                    $"Port must be between {MinimumPort} and {MaximumPort}.");
            }
        }

        var duplicate = assignments
            .GroupBy(assignment => assignment.Value)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException(
                $"Port {duplicate.Key} is assigned to more than one Portable Developer service.",
                nameof(settings));
        }

        return settings;
    }
}
