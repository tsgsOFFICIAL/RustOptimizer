using Avalonia.Markup.Xaml.MarkupExtensions;
using System.Text.RegularExpressions;
using Avalonia.Controls.Documents;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Input;
using Avalonia;

namespace RustOptimizer;

/// <summary>
/// Renders a small, changelog-oriented subset of Markdown (headings, bullet lists, rules,
/// bold/italic/code spans, links and images/GIFs) directly into Avalonia controls, without
/// pulling in a full CommonMark dependency.
/// </summary>
public static partial class MarkdownRenderer
{
    [GeneratedRegex(@"\*\*(?<bold>.+?)\*\*|\*(?<italic>.+?)\*|`(?<code>.+?)`|\[(?<linktext>.+?)\]\((?<linkurl>.+?)\)")]
    private static partial Regex InlineTokenRegex();

    /// <summary>
    /// Matches a line that is entirely a Markdown image, e.g. "![Demo](https://example.com/demo.gif)".
    /// Only matched as a standalone block (not inline within other text), since a changelog image is
    /// almost always on its own line.
    /// </summary>
    [GeneratedRegex(@"^!\[(?<alt>.*?)\]\((?<url>.+?)\)$")]
    private static partial Regex ImageLineRegex();

    /// <summary>
    /// Renders the given Markdown text into a scrollable panel of controls.
    /// </summary>
    /// <param name="markdown">The Markdown source to render.</param>
    public static Control Render(string markdown)
    {
        StackPanel panel = new() { Spacing = 4 };

        foreach (string rawLine in markdown.Replace("\r\n", "\n").Split('\n'))
        {
            string line = rawLine.Trim();

            if (line.Length == 0)
            {
                panel.Children.Add(new Border { Height = 6 });
                continue;
            }

            if (IsRule(line))
            {
                Border rule = new() { Height = 1, Margin = new Thickness(0, 8) };
                rule.Bind(Border.BackgroundProperty, new DynamicResourceExtension("BorderColor"));
                panel.Children.Add(rule);
                continue;
            }

            Match imageMatch = ImageLineRegex().Match(line);
            if (imageMatch.Success)
            {
                panel.Children.Add(AnimatedImage.Create(imageMatch.Groups["url"].Value, imageMatch.Groups["alt"].Value));
                continue;
            }

            if (line.StartsWith("### "))
            {
                panel.Children.Add(CreateTextBlock(line[4..], "changelogH3"));
                continue;
            }

            if (line.StartsWith("## "))
            {
                panel.Children.Add(CreateTextBlock(line[3..], "changelogH2"));
                continue;
            }

            if (line.StartsWith("# "))
            {
                panel.Children.Add(CreateTextBlock(line[2..], "changelogH1"));
                continue;
            }

            if (line.StartsWith("- ") || line.StartsWith("* "))
            {
                panel.Children.Add(CreateListItem(line[2..]));
                continue;
            }

            panel.Children.Add(CreateTextBlock(line, "changelogBody"));
        }

        return new ScrollViewer { Content = panel };
    }

    /// <summary>
    /// Determines whether the line is a horizontal rule (three or more repeated "-", "*" or "_").
    /// </summary>
    private static bool IsRule(string line)
    {
        if (line.Length < 3)
            return false;

        char first = line[0];
        if (first is not ('-' or '*' or '_'))
            return false;

        foreach (char c in line)
        {
            if (c != first)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Creates a wrapping, inline-formatted text block styled with the given CSS-like class.
    /// </summary>
    private static TextBlock CreateTextBlock(string text, string styleClass)
    {
        TextBlock block = new() { TextWrapping = TextWrapping.Wrap };
        block.Classes.Add(styleClass);

        foreach (Inline inline in ParseInlines(text))
            block.Inlines!.Add(inline);

        return block;
    }

    /// <summary>
    /// Creates a bullet list item: a leading "•" marker beside a wrapping, inline-formatted text block.
    /// </summary>
    private static Control CreateListItem(string text)
    {
        TextBlock bullet = new() { Text = "•", Margin = new Thickness(4, 0, 8, 0) };
        bullet.Classes.Add("changelogBody");

        TextBlock content = CreateTextBlock(text, "changelogBody");

        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(8, 0, 0, 0),
            Children = { bullet, content }
        };
    }

    /// <summary>
    /// Tokenizes a single line of Markdown into bold/italic/code/link/plain inline runs, in order.
    /// </summary>
    private static IEnumerable<Inline> ParseInlines(string text)
    {
        List<Inline> inlines = new();
        int cursor = 0;

        foreach (Match match in InlineTokenRegex().Matches(text))
        {
            if (match.Index > cursor)
                inlines.Add(new Run { Text = text[cursor..match.Index] });

            if (match.Groups["bold"].Success)
                inlines.Add(new Run { Text = match.Groups["bold"].Value, FontWeight = FontWeight.Bold });
            else if (match.Groups["italic"].Success)
                inlines.Add(new Run { Text = match.Groups["italic"].Value, FontStyle = FontStyle.Italic });
            else if (match.Groups["code"].Success)
                inlines.Add(new Run { Text = match.Groups["code"].Value, FontFamily = new FontFamily("Consolas,Menlo,monospace") });
            else if (match.Groups["linkurl"].Success)
                inlines.Add(CreateLink(match.Groups["linktext"].Value, match.Groups["linkurl"].Value));

            cursor = match.Index + match.Length;
        }

        if (cursor < text.Length)
            inlines.Add(new Run { Text = text[cursor..] });

        return inlines;
    }

    /// <summary>
    /// Creates a clickable link inline that opens the target URL in the system's default handler.
    /// </summary>
    private static Inline CreateLink(string text, string url)
    {
        TextBlock link = new()
        {
            Text = text,
            TextDecorations = TextDecorations.Underline,
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        link.Bind(TextBlock.ForegroundProperty, new DynamicResourceExtension("AccentColor"));

        link.PointerPressed += (_, _) => Utility.OpenUrl(url);

        return new InlineUIContainer { Child = link };
    }
}