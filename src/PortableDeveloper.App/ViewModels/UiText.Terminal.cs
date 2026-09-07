namespace PortableDeveloper.App.ViewModels;

public sealed partial class UiText
{
    public string TerminalProcessTimedOut => IsCzech
        ? "Proces překročil maximální dobu běhu a byl ukončen."
        : "The process exceeded its maximum runtime and was stopped.";

    public string TerminalProcessExited(int? exitCode) => IsCzech
        ? $"Proces skončil s kódem {exitCode?.ToString() ?? "?"}."
        : $"The process exited with code {exitCode?.ToString() ?? "?"}.";

    public string TerminalOutputTruncated => IsCzech
        ? "… Starší výstup terminálu byl odebrán, aby konzole zůstala svižná."
        : "… Older terminal output was removed to keep the console responsive.";

}
