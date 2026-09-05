using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace PortableDeveloper.App.Guides;

internal static class MarkdownGuideRenderer
{
    public static FlowDocument Render(string markdown, bool isCzech)
    {
        var document = new FlowDocument
        {
            PagePadding = new Thickness(0),
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 14,
            LineHeight = 22
        };
        document.SetResourceReference(TextElement.ForegroundProperty, "AppForegroundBrush");

        var paragraph = new StringBuilder();
        var code = new StringBuilder();
        List? list = null;
        var inCode = false;
        var codeLanguage = string.Empty;

        foreach (var rawLine in NormalizeLines(markdown))
        {
            var line = rawLine.TrimEnd();
            if (line.StartsWith("```", StringComparison.Ordinal))
            {
                FlushParagraph(document, paragraph);
                FlushList(document, ref list);
                if (inCode)
                {
                    document.Blocks.Add(CreateCodeBlock(code.ToString().TrimEnd('\r', '\n'), codeLanguage, isCzech));
                    code.Clear();
                }
                else
                {
                    codeLanguage = NormalizeCodeLanguage(line[3..]);
                }

                inCode = !inCode;
                continue;
            }

            if (inCode)
            {
                code.AppendLine(rawLine);
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                FlushParagraph(document, paragraph);
                FlushList(document, ref list);
                continue;
            }

            if (TryGetHeading(line, out var level, out var heading))
            {
                FlushParagraph(document, paragraph);
                FlushList(document, ref list);
                document.Blocks.Add(CreateHeading(heading, level));
                continue;
            }

            if (line.StartsWith("> ", StringComparison.Ordinal))
            {
                FlushParagraph(document, paragraph);
                FlushList(document, ref list);
                document.Blocks.Add(CreateNotice(line[2..]));
                continue;
            }

            if (TryGetTags(line, out var tags))
            {
                FlushParagraph(document, paragraph);
                FlushList(document, ref list);
                document.Blocks.Add(CreateTags(tags));
                continue;
            }

            if (TryGetListItem(line, out var ordered, out var itemText))
            {
                FlushParagraph(document, paragraph);
                if (list is null || list.MarkerStyle != (ordered ? TextMarkerStyle.Decimal : TextMarkerStyle.Disc))
                {
                    FlushList(document, ref list);
                    list = new List
                    {
                        MarkerStyle = ordered ? TextMarkerStyle.Decimal : TextMarkerStyle.Disc,
                        Margin = new Thickness(22, 5, 0, 12)
                    };
                }

                list.ListItems.Add(new ListItem(new Paragraph(new Run(itemText)))
                {
                    Margin = new Thickness(0, 2, 0, 2)
                });
                continue;
            }

            if (paragraph.Length > 0)
            {
                paragraph.Append(' ');
            }

            paragraph.Append(line);
        }

        if (inCode && code.Length > 0)
        {
            document.Blocks.Add(CreateCodeBlock(code.ToString().TrimEnd('\r', '\n'), codeLanguage, isCzech));
        }

        FlushParagraph(document, paragraph);
        FlushList(document, ref list);
        return document;
    }

    private static IEnumerable<string> NormalizeLines(string markdown) =>
        markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');

    private static string NormalizeCodeLanguage(string value)
    {
        var language = value.Trim();
        return language.Length is > 0 and <= 24
            && language.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '+' or '#')
                ? language
                : string.Empty;
    }

    private static bool TryGetHeading(string line, out int level, out string text)
    {
        level = 0;
        while (level < line.Length && line[level] == '#')
        {
            level++;
        }

        if (level is < 1 or > 3 || level >= line.Length || line[level] != ' ')
        {
            text = string.Empty;
            return false;
        }

        text = line[(level + 1)..].Trim();
        return text.Length > 0;
    }

    private static bool TryGetListItem(string line, out bool ordered, out string text)
    {
        if (line.StartsWith("- ", StringComparison.Ordinal))
        {
            ordered = false;
            text = line[2..].Trim();
            return text.Length > 0;
        }

        var dot = line.IndexOf(". ", StringComparison.Ordinal);
        if (dot > 0 && int.TryParse(line[..dot], out _))
        {
            ordered = true;
            text = line[(dot + 2)..].Trim();
            return text.Length > 0;
        }

        ordered = false;
        text = string.Empty;
        return false;
    }

    private static bool TryGetTags(string line, out IReadOnlyList<string> tags)
    {
        var separator = line.StartsWith("Tags:", StringComparison.OrdinalIgnoreCase)
            ? line.IndexOf(':')
            : line.StartsWith("Štítky:", StringComparison.OrdinalIgnoreCase)
                ? line.IndexOf(':')
                : -1;
        if (separator < 0)
        {
            tags = [];
            return false;
        }

        tags = line[(separator + 1)..]
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(tag => tag.Length is > 0 and <= 32)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .ToArray();
        return tags.Count > 0;
    }

    private static Paragraph CreateHeading(string text, int level)
    {
        var heading = new Paragraph(new Run(text))
        {
            FontWeight = FontWeights.SemiBold,
            FontSize = level switch { 1 => 25, 2 => 19, _ => 16 },
            Margin = new Thickness(0, level == 1 ? 0 : 18, 0, 8),
            KeepWithNext = true
        };
        heading.SetResourceReference(TextElement.ForegroundProperty, "AppStrongForegroundBrush");
        return heading;
    }

    private static void FlushParagraph(FlowDocument document, StringBuilder content)
    {
        if (content.Length == 0)
        {
            return;
        }

        var paragraph = new Paragraph(new Run(content.ToString()))
        {
            Margin = new Thickness(0, 0, 0, 12)
        };
        paragraph.SetResourceReference(TextElement.ForegroundProperty, "AppMutedBrush");
        document.Blocks.Add(paragraph);
        content.Clear();
    }

    private static void FlushList(FlowDocument document, ref List? list)
    {
        if (list is null)
        {
            return;
        }

        document.Blocks.Add(list);
        list = null;
    }

    private static BlockUIContainer CreateNotice(string text)
    {
        var message = new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            LineHeight = 21
        };
        message.SetResourceReference(TextBlock.ForegroundProperty, "AppForegroundBrush");

        var border = new Border
        {
            Padding = new Thickness(14, 11, 14, 11),
            Margin = new Thickness(0, 2, 0, 14),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Child = message
        };
        border.SetResourceReference(Border.BackgroundProperty, "AppIconSurfaceBrush");
        border.SetResourceReference(Border.BorderBrushProperty, "AppAccentBorderBrush");
        return new BlockUIContainer(border);
    }

    private static BlockUIContainer CreateTags(IEnumerable<string> tags)
    {
        var panel = new WrapPanel
        {
            Margin = new Thickness(0, 0, 0, 10)
        };
        foreach (var tag in tags)
        {
            var label = new TextBlock
            {
                Text = tag,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold
            };
            label.SetResourceReference(TextBlock.ForegroundProperty, "AppInfoBrush");

            var chip = new Border
            {
                Padding = new Thickness(9, 4, 9, 4),
                Margin = new Thickness(0, 0, 7, 6),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Child = label
            };
            chip.SetResourceReference(Border.BackgroundProperty, "AppIconSurfaceBrush");
            chip.SetResourceReference(Border.BorderBrushProperty, "AppButtonBorderBrush");
            panel.Children.Add(chip);
        }

        return new BlockUIContainer(panel);
    }

    private static BlockUIContainer CreateCodeBlock(string code, string language, bool isCzech)
    {
        var editor = new TextBox
        {
            Text = code,
            IsReadOnly = true,
            AcceptsReturn = true,
            AcceptsTab = true,
            TextWrapping = TextWrapping.NoWrap,
            FontFamily = new FontFamily("Cascadia Mono, Consolas"),
            FontSize = 13,
            Padding = new Thickness(13),
            BorderThickness = new Thickness(0),
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            MaxHeight = 430
        };
        editor.SetResourceReference(Control.ForegroundProperty, "AppForegroundBrush");
        editor.SetResourceReference(Control.BackgroundProperty, "AppDeepSurfaceBrush");

        var copyButton = new Button
        {
            Content = isCzech ? "Kopírovat" : "Copy",
            Padding = new Thickness(12, 6, 12, 6),
            HorizontalAlignment = HorizontalAlignment.Right,
            Cursor = Cursors.Hand
        };
        copyButton.SetResourceReference(FrameworkElement.StyleProperty, "AppButtonStyle");
        copyButton.Click += (_, _) =>
        {
            try
            {
                Clipboard.SetText(code);
            }
            catch (System.Runtime.InteropServices.ExternalException)
            {
                editor.Focus();
                editor.SelectAll();
            }
        };

        var header = new Grid
        {
            Margin = new Thickness(0, 0, 0, 8)
        };
        header.ColumnDefinitions.Add(new ColumnDefinition());
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var languageLabel = new TextBlock
        {
            Text = string.IsNullOrEmpty(language) ? (isCzech ? "Kód" : "Code") : language,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 11,
            FontWeight = FontWeights.SemiBold
        };
        languageLabel.SetResourceReference(TextBlock.ForegroundProperty, "AppSubtleBrush");
        Grid.SetColumn(copyButton, 1);
        header.Children.Add(languageLabel);
        header.Children.Add(copyButton);

        var panel = new Grid();
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition());
        Grid.SetRow(editor, 1);
        panel.Children.Add(header);
        panel.Children.Add(editor);

        var border = new Border
        {
            Padding = new Thickness(10),
            Margin = new Thickness(0, 2, 0, 14),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(7),
            Child = panel
        };
        border.SetResourceReference(Border.BackgroundProperty, "AppDeepSurfaceBrush");
        border.SetResourceReference(Border.BorderBrushProperty, "AppButtonBorderBrush");
        return new BlockUIContainer(border);
    }
}
