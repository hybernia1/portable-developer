using PortableDeveloper.Application.Scheduling;

namespace PortableDeveloper.App.ViewModels;

public sealed partial class UiText
{
    public string SchedulerTasksTab => IsCzech ? "Úlohy" : "Tasks";

    public string SchedulerHistoryTab => IsCzech ? "Historie" : "History";

    public string NewScheduledTask => IsCzech ? "Nová úloha" : "New task";

    public string EditScheduledTask => IsCzech ? "Upravit" : "Edit";

    public string DeleteScheduledTask => IsCzech ? "Odstranit" : "Delete";

    public string RunScheduledTaskNow => IsCzech ? "Spustit nyní" : "Run now";

    public string NoScheduledTasks => IsCzech
        ? "Aktivní projekt zatím nemá žádné naplánované úlohy."
        : "The active project does not have any scheduled tasks yet.";

    public string NoScheduledTaskHistory => IsCzech
        ? "Aktivní projekt zatím nemá žádnou historii běhů."
        : "The active project does not have any run history yet.";

    public string NoMatchingScheduledTaskHistory => IsCzech
        ? "Filtru neodpovídá žádný záznam historie."
        : "No history record matches the filter.";

    public string ScheduledTaskHistoryFilter => IsCzech ? "Filtrovat historii" : "Filter history";

    public string ScheduledTaskHistoryFilterHint => IsCzech
        ? "Hledá v názvu, čase, spouštěči, výsledku i textu výstupu."
        : "Searches task name, time, trigger, result, and captured output.";

    public string ClearScheduledTaskHistoryFilter => IsCzech ? "Zrušit filtr" : "Clear filter";

    public string ViewScheduledTaskLog => IsCzech ? "Detail" : "Details";

    public string DeleteScheduledTaskLog => IsCzech ? "Smazat" : "Delete";

    public string ClearScheduledTaskHistory => IsCzech ? "Smazat historii" : "Clear history";

    public string ScheduledTaskLogDetailsTitle => IsCzech ? "Detail běhu úlohy" : "Task run details";

    public string CloseScheduledTaskLog => IsCzech ? "Zavřít" : "Close";

    public string ScheduledTaskLogHasNoOutput => IsCzech
        ? "Běh nezachytil žádný výstup."
        : "The run did not capture any output.";

    public string ScheduledTaskLogDeleteConfirmation(string taskName, string started) => IsCzech
        ? $"Smazat záznam běhu úlohy „{taskName}“ z {started}?"
        : $"Delete the {taskName} run recorded at {started}?";

    public string ScheduledTaskHistoryClearConfirmation(int count) => IsCzech
        ? $"Smazat všech {count} záznamů historie aktivního projektu? Tuto akci nelze vrátit."
        : $"Delete all {count} history records for the active project? This cannot be undone.";

    public string ScheduledTaskLogDeleted => IsCzech
        ? "Záznam historie byl smazán."
        : "The history record was deleted.";

    public string ScheduledTaskHistoryCleared(int count) => IsCzech
        ? $"Historie byla smazána ({count} záznamů)."
        : $"History was cleared ({count} records).";

    public string SchedulerRunsOnlyWhileOpen => IsCzech
        ? "Úlohy se spouštějí pouze během běhu Portable Developeru. Zameškané běhy se nepouštějí zpětně."
        : "Tasks run only while Portable Developer is open. Missed runs are not replayed.";

    public string ScheduledTaskName => IsCzech ? "Název" : "Name";

    public string ScheduledTaskCommand => IsCzech ? "Typ" : "Type";

    public string ScheduledTaskTarget => IsCzech ? "Skript / npm úloha" : "Script / npm task";

    public string ScheduledTaskSchedule => IsCzech ? "Plán" : "Schedule";

    public string ScheduledTaskNextRun => IsCzech ? "Příští běh" : "Next run";

    public string ScheduledTaskLastRun => IsCzech ? "Poslední běh" : "Last run";

    public string ScheduledTaskStatus => IsCzech ? "Stav" : "Status";

    public string ScheduledTaskOutput => IsCzech ? "Výstup" : "Output";

    public string ScheduledTaskStarted => IsCzech ? "Spuštěno" : "Started";

    public string ScheduledTaskDuration => IsCzech ? "Doba" : "Duration";

    public string ScheduledTaskTriggerLabel => IsCzech ? "Spouštěč" : "Trigger";

    public string ScheduledTaskResult => IsCzech ? "Výsledek" : "Result";

    public string ScheduledTaskEnabled => IsCzech ? "Zapnutá" : "Enabled";

    public string ScheduledTaskDisabled => IsCzech ? "Vypnutá" : "Disabled";

    public string ScheduledTaskRunning => IsCzech ? "Právě běží" : "Running";

    public string ScheduledTaskNever => IsCzech ? "Nikdy" : "Never";

    public string ScheduledTaskNotScheduled => IsCzech ? "—" : "—";

    public string ScheduledTaskDialogTitle(bool editing) => IsCzech
        ? editing ? "Upravit naplánovanou úlohu" : "Nová naplánovaná úloha"
        : editing ? "Edit scheduled task" : "New scheduled task";

    public string ScheduledTaskTargetHelp => IsCzech
        ? "U skriptů zadejte cestu relativní ke kořeni projektu. U npm zadejte název skriptu z package.json."
        : "For scripts, enter a path relative to the project root. For npm, enter a script name from package.json.";

    public string ScheduledTaskArguments => IsCzech ? "Argumenty" : "Arguments";

    public string ScheduledTaskArgumentsHelp => IsCzech
        ? "Volitelné argumenty. Uvozovky zachovají mezery; nepoužívá se systémový shell."
        : "Optional arguments. Quotes preserve spaces; no system shell is used.";

    public string ScheduledTaskIntervalMinutes => IsCzech ? "Interval v minutách" : "Interval in minutes";

    public string ScheduledTaskTime => IsCzech ? "Čas (HH:mm)" : "Time (HH:mm)";

    public string ScheduledTaskDay => IsCzech ? "Den" : "Day";

    public string ScheduledTaskTimeout => IsCzech ? "Timeout v minutách" : "Timeout in minutes";

    public string SaveScheduledTask => IsCzech ? "Uložit úlohu" : "Save task";

    public string ScheduledTaskDeleteConfirmation(string name) => IsCzech
        ? $"Opravdu odstranit úlohu „{name}“? Její historie zůstane zachovaná."
        : $"Delete the task “{name}”? Its run history will be preserved.";

    public string ScheduledTaskSaved => IsCzech ? "Naplánovaná úloha byla uložena." : "The scheduled task was saved.";

    public string ScheduledTaskDeleted => IsCzech ? "Naplánovaná úloha byla odstraněna." : "The scheduled task was deleted.";

    public string ScheduledTaskOperationFailed(string detail) => IsCzech
        ? $"Operace plánovače selhala: {detail}"
        : $"Scheduler operation failed: {detail}";

    public string ScheduledTaskValidationFailed => IsCzech
        ? "Zkontrolujte název, relativní cestu nebo npm skript, plán a číselné hodnoty."
        : "Check the name, relative path or npm script, schedule, and numeric values.";

    public string ScheduledTaskCompleted(ScheduledTaskOutcome outcome) => IsCzech
        ? $"Úloha skončila: {ScheduledTaskOutcomeLabel(outcome)}."
        : $"Task finished: {ScheduledTaskOutcomeLabel(outcome)}.";

    public string ScheduledTaskCommandLabel(ScheduledTaskCommandKind kind) => kind switch
    {
        ScheduledTaskCommandKind.PhpScript => "PHP",
        ScheduledTaskCommandKind.PythonScript => "Python",
        ScheduledTaskCommandKind.NodeScript => "Node.js",
        ScheduledTaskCommandKind.NpmScript => "npm run",
        _ => kind.ToString()
    };

    public string ScheduledTaskScheduleLabel(ScheduledTaskSchedule schedule) => schedule.Kind switch
    {
        ScheduledTaskScheduleKind.ApplicationStart => IsCzech ? "Při spuštění aplikace" : "When the application starts",
        ScheduledTaskScheduleKind.Interval => IsCzech
            ? $"Každých {schedule.IntervalMinutes} min"
            : $"Every {schedule.IntervalMinutes} min",
        ScheduledTaskScheduleKind.Daily => IsCzech
            ? $"Denně v {schedule.Hour:00}:{schedule.Minute:00}"
            : $"Daily at {schedule.Hour:00}:{schedule.Minute:00}",
        ScheduledTaskScheduleKind.Weekly => IsCzech
            ? $"{ScheduledTaskDayLabel(schedule.DayOfWeek)} v {schedule.Hour:00}:{schedule.Minute:00}"
            : $"{ScheduledTaskDayLabel(schedule.DayOfWeek)} at {schedule.Hour:00}:{schedule.Minute:00}",
        _ => schedule.Kind.ToString()
    };

    public string ScheduledTaskScheduleKindLabel(ScheduledTaskScheduleKind kind) => kind switch
    {
        ScheduledTaskScheduleKind.ApplicationStart => IsCzech ? "Při spuštění aplikace" : "Application start",
        ScheduledTaskScheduleKind.Interval => IsCzech ? "Interval" : "Interval",
        ScheduledTaskScheduleKind.Daily => IsCzech ? "Denně" : "Daily",
        ScheduledTaskScheduleKind.Weekly => IsCzech ? "Týdně" : "Weekly",
        _ => kind.ToString()
    };

    public string ScheduledTaskDayLabel(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => IsCzech ? "Pondělí" : "Monday",
        DayOfWeek.Tuesday => IsCzech ? "Úterý" : "Tuesday",
        DayOfWeek.Wednesday => IsCzech ? "Středa" : "Wednesday",
        DayOfWeek.Thursday => IsCzech ? "Čtvrtek" : "Thursday",
        DayOfWeek.Friday => IsCzech ? "Pátek" : "Friday",
        DayOfWeek.Saturday => IsCzech ? "Sobota" : "Saturday",
        DayOfWeek.Sunday => IsCzech ? "Neděle" : "Sunday",
        _ => day.ToString()
    };

    public string ScheduledTaskOutcomeLabel(ScheduledTaskOutcome outcome) => outcome switch
    {
        ScheduledTaskOutcome.Succeeded => IsCzech ? "Úspěch" : "Succeeded",
        ScheduledTaskOutcome.Failed => IsCzech ? "Selhalo" : "Failed",
        ScheduledTaskOutcome.TimedOut => "Timeout",
        ScheduledTaskOutcome.Canceled => IsCzech ? "Zrušeno" : "Canceled",
        _ => outcome.ToString()
    };

    public string ScheduledTaskTrigger(ScheduledTaskTrigger trigger) => trigger switch
    {
        Application.Scheduling.ScheduledTaskTrigger.Manual => IsCzech ? "Ručně" : "Manual",
        Application.Scheduling.ScheduledTaskTrigger.ApplicationStart => IsCzech ? "Start aplikace" : "Application start",
        _ => IsCzech ? "Plán" : "Schedule"
    };

}
