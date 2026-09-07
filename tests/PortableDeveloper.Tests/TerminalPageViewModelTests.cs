using PortableDeveloper.App.ViewModels;
using PortableDeveloper.Application.Settings;

namespace PortableDeveloper.Tests;

public sealed class TerminalPageViewModelTests
{
    [Fact]
    public void Prompt_input_and_history_survive_view_recreation_in_the_page_model()
    {
        var page = new TerminalPageViewModel(new UiText(new InMemorySettingsStore()));
        page.WritePrompt("project:/src");
        page.TextContent += "first";

        Assert.Equal("first", page.CurrentInput);

        page.RecordCommand(page.CurrentInput);
        page.AppendRaw(Environment.NewLine);
        page.WritePrompt("project:/src");
        page.NavigateHistory(-1);

        Assert.Equal("first", page.CurrentInput);
    }

    [Fact]
    public void Process_output_is_inserted_before_interactive_session_input()
    {
        var page = new TerminalPageViewModel(new UiText(new InMemorySettingsStore()));
        page.WritePrompt("project:/");
        page.AppendRaw("typed input");
        page.MarkInputStart();
        page.AppendRaw("next input");

        page.AppendProcessOutput("server output" + Environment.NewLine);

        Assert.EndsWith("next input", page.TextContent, StringComparison.Ordinal);
        Assert.Contains("server output", page.TextContent, StringComparison.Ordinal);
        Assert.Equal("next input", page.CurrentInput);
    }

    private sealed class InMemorySettingsStore : IApplicationSettingsStore
    {
        private ApplicationSettings _settings = ApplicationSettings.Default;

        public ApplicationSettings Load() => _settings;

        public void Save(ApplicationSettings settings) => _settings = settings;
    }
}
