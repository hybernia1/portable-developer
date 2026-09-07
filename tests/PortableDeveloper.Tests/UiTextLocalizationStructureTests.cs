using PortableDeveloper.App.ViewModels;
using PortableDeveloper.Application.Settings;
using System.Text.RegularExpressions;

namespace PortableDeveloper.Tests;

public sealed class UiTextLocalizationStructureTests
{
    [Fact]
    public void Feature_partial_files_keep_one_runtime_switchable_text_contract()
    {
        var store = new InMemorySettingsStore(
            ApplicationSettings.Default with { Language = ApplicationLanguage.English });
        var text = new UiText(store);
        var notifications = new List<string?>();
        text.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);

        Assert.Equal("Projects", text.ProjectsTab);
        Assert.Equal("Tasks", text.SchedulerTasksTab);
        Assert.Equal("Managed browsers", text.InstalledSeleniumDrivers);

        text.SetLanguage(ApplicationLanguage.Czech);

        Assert.Equal("Projekty", text.ProjectsTab);
        Assert.Equal("Úlohy", text.SchedulerTasksTab);
        Assert.Equal("Spravované browsery", text.InstalledSeleniumDrivers);
        Assert.Equal(ApplicationLanguage.Czech, store.Load().Language);
        Assert.Contains(string.Empty, notifications);
    }

    [Fact]
    public void Localization_contract_is_split_by_feature_instead_of_returning_to_one_monolith()
    {
        var repositoryRoot = FindRepositoryRoot();
        var viewModelsRoot = Path.Combine(repositoryRoot, "src", "PortableDeveloper.App", "ViewModels");
        var corePath = Path.Combine(viewModelsRoot, "UiText.cs");
        var featureFiles = Directory.GetFiles(viewModelsRoot, "UiText.*.cs");

        Assert.True(File.ReadLines(corePath).Count() < 200);
        Assert.True(featureFiles.Length >= 10);
        Assert.All(featureFiles, path =>
        {
            Assert.Contains("partial class UiText", File.ReadAllText(path), StringComparison.Ordinal);
            Assert.True(File.ReadLines(path).Count() < 350, $"Localization file is too broad: {Path.GetFileName(path)}");
        });
    }

    [Fact]
    public void Localization_members_have_application_consumers()
    {
        var repositoryRoot = FindRepositoryRoot();
        var appRoot = Path.Combine(repositoryRoot, "src", "PortableDeveloper.App");
        var sourceFiles = Directory
            .EnumerateFiles(appRoot, "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var combinedSource = string.Join(Environment.NewLine, sourceFiles.Select(File.ReadAllText));
        var identifierCounts = Regex.Matches(combinedSource, "\\b[A-Za-z_][A-Za-z0-9_]*\\b")
            .GroupBy(match => match.Value, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var viewModelsRoot = Path.Combine(appRoot, "ViewModels");
        var unusedMembers = Directory
            .EnumerateFiles(viewModelsRoot, "UiText*.cs")
            .SelectMany(path => Regex.Matches(
                    File.ReadAllText(path),
                    "(?m)^\\s*public\\s+string\\s+([A-Za-z_][A-Za-z0-9_]*)")
                .Select(match => new { Path = path, Name = match.Groups[1].Value }))
            .Where(member => identifierCounts.GetValueOrDefault(member.Name) == 1)
            .Select(member => $"{Path.GetRelativePath(repositoryRoot, member.Path)}: {member.Name}")
            .ToArray();

        Assert.True(
            unusedMembers.Length == 0,
            $"Localization members must have an application consumer.{Environment.NewLine}{string.Join(Environment.NewLine, unusedMembers)}");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "PortableDeveloper.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("PortableDeveloper.slnx was not found above the test output directory.");
    }

    private sealed class InMemorySettingsStore(ApplicationSettings settings) : IApplicationSettingsStore
    {
        private ApplicationSettings _settings = settings;

        public ApplicationSettings Load() => _settings;

        public void Save(ApplicationSettings settings) => _settings = settings;
    }
}
