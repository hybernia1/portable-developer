namespace PortableDeveloper.Application.Scheduling;

public static class ScheduledTaskValidator
{
    public const int MaximumNameLength = 100;
    public const int MaximumTargetLength = 500;
    public const int MaximumArgumentsLength = 4096;
    public const int MinimumIntervalMinutes = 1;
    public const int MaximumIntervalMinutes = 43_200;
    public const int MinimumTimeoutMinutes = 1;
    public const int MaximumTimeoutMinutes = 1_440;

    public static PortableScheduledTask Validate(PortableScheduledTask task)
    {
        ArgumentNullException.ThrowIfNull(task);
        var id = ValidateIdentifier(task.Id, nameof(task.Id));
        var projectId = ValidateIdentifier(task.ProjectId, nameof(task.ProjectId));
        var name = RequireBounded(task.Name, MaximumNameLength, "A task name is required.");
        var target = RequireBounded(task.Target, MaximumTargetLength, "A script path or npm script name is required.");
        var arguments = (task.Arguments ?? string.Empty).Trim();
        if (arguments.Length > MaximumArgumentsLength)
        {
            throw new ArgumentException($"Task arguments cannot exceed {MaximumArgumentsLength} characters.", nameof(task));
        }

        if (!Enum.IsDefined(task.CommandKind))
        {
            throw new ArgumentException("The task command type is invalid.", nameof(task));
        }

        ValidateSchedule(task.Schedule);
        if (task.TimeoutMinutes is < MinimumTimeoutMinutes or > MaximumTimeoutMinutes)
        {
            throw new ArgumentException($"The timeout must be between {MinimumTimeoutMinutes} and {MaximumTimeoutMinutes} minutes.", nameof(task));
        }

        if (task.CommandKind == ScheduledTaskCommandKind.NpmScript)
        {
            if (target.Length > 128 || !char.IsLetterOrDigit(target[0]) ||
                target.Any(character => !char.IsLetterOrDigit(character) && character is not '.' and not '_' and not '-' and not ':'))
            {
                throw new ArgumentException("The npm script name contains unsupported characters.", nameof(task));
            }
        }
        else
        {
            if (Path.IsPathRooted(target))
            {
                throw new ArgumentException("Scheduled script paths must be relative to the project.", nameof(task));
            }

            var normalized = target.Replace('/', Path.DirectorySeparatorChar);
            if (normalized.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(segment => segment is ".." or "." || string.IsNullOrWhiteSpace(segment)))
            {
                throw new ArgumentException("The scheduled script path is invalid.", nameof(task));
            }

            target = normalized;
        }

        return task with { Id = id, ProjectId = projectId, Name = name, Target = target, Arguments = arguments };
    }

    public static void ValidateSchedule(ScheduledTaskSchedule schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        if (!Enum.IsDefined(schedule.Kind) || !Enum.IsDefined(schedule.DayOfWeek))
        {
            throw new ArgumentException("The task schedule is invalid.", nameof(schedule));
        }

        if (schedule.Kind == ScheduledTaskScheduleKind.Interval &&
            schedule.IntervalMinutes is < MinimumIntervalMinutes or > MaximumIntervalMinutes)
        {
            throw new ArgumentException($"The interval must be between {MinimumIntervalMinutes} and {MaximumIntervalMinutes} minutes.", nameof(schedule));
        }

        if (schedule.Hour is < 0 or > 23 || schedule.Minute is < 0 or > 59)
        {
            throw new ArgumentException("The scheduled time is invalid.", nameof(schedule));
        }
    }

    private static string ValidateIdentifier(string value, string parameterName)
    {
        value = RequireBounded(value, 64, "An identifier is required.");
        if (!char.IsLetterOrDigit(value[0]) ||
            value.Any(character => !char.IsLetterOrDigit(character) && character is not '-' and not '_'))
        {
            throw new ArgumentException("Identifiers may contain only letters, numbers, hyphens, and underscores.", parameterName);
        }

        return value;
    }

    private static string RequireBounded(string? value, int maximumLength, string message)
    {
        value = value?.Trim() ?? string.Empty;
        if (value.Length is 0 || value.Length > maximumLength)
        {
            throw new ArgumentException(message);
        }

        return value;
    }
}
