using System;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace Prism.Controls;

/// <summary>
/// Minimal, dependency-free Markdown-to-inlines renderer for chat bubbles.
/// Handles headings (#, ##, ###), bullets (*, -, •), **bold**, *italic* and `code`.
/// Applied via the attached <c>Text</c> property so it re-renders as text streams in.
/// </summary>
public sealed class Markdown
{
    private Markdown() { }

    public static readonly AttachedProperty<string?> TextProperty =
        AvaloniaProperty.RegisterAttached<Markdown, TextBlock, string?>("Text");

    public static void SetText(TextBlock element, string? value) =>
        element.SetValue(TextProperty, value);

    public static string? GetText(TextBlock element) =>
        element.GetValue(TextProperty);

    static Markdown()
    {
        TextProperty.Changed.AddClassHandler<TextBlock>((tb, e) =>
            Render(tb, e.NewValue as string));
    }

    private static void Render(TextBlock tb, string? markdown)
    {
        var inlines = tb.Inlines;
        if (inlines is null)
            return;

        inlines.Clear();
        if (string.IsNullOrEmpty(markdown))
            return;

        var lines = markdown
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal)
            .Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0)
                inlines.Add(new LineBreak());
            ProcessLine(inlines, lines[i]);
        }
    }

    private static void ProcessLine(InlineCollection inlines, string raw)
    {
        var content = raw.Trim();
        if (content.Length == 0)
            return;

        double? size = null;
        bool heading = false;

        if (content.StartsWith("### ", StringComparison.Ordinal))
        {
            size = 15; heading = true; content = content[4..];
        }
        else if (content.StartsWith("## ", StringComparison.Ordinal))
        {
            size = 17; heading = true; content = content[3..];
        }
        else if (content.StartsWith("# ", StringComparison.Ordinal))
        {
            size = 20; heading = true; content = content[2..];
        }
        else if (content.StartsWith("* ", StringComparison.Ordinal) ||
                 content.StartsWith("- ", StringComparison.Ordinal) ||
                 content.StartsWith("• ", StringComparison.Ordinal))
        {
            inlines.Add(new Run("•  "));
            content = content[2..];
        }

        ParseInline(inlines, content, size, heading);
    }

    private static void ParseInline(InlineCollection inlines, string text, double? size, bool forceBold)
    {
        var plain = new StringBuilder();

        void FlushPlain()
        {
            if (plain.Length == 0)
                return;
            inlines.Add(MakeRun(plain.ToString(), forceBold, false, false, size));
            plain.Clear();
        }

        int i = 0;
        while (i < text.Length)
        {
            // **bold**
            if (i + 1 < text.Length && text[i] == '*' && text[i + 1] == '*')
            {
                int close = text.IndexOf("**", i + 2, StringComparison.Ordinal);
                if (close > 0)
                {
                    FlushPlain();
                    inlines.Add(MakeRun(text[(i + 2)..close], true, false, false, size));
                    i = close + 2;
                    continue;
                }
            }

            // *italic*
            if (text[i] == '*')
            {
                int close = text.IndexOf('*', i + 1);
                if (close > i + 1)
                {
                    FlushPlain();
                    inlines.Add(MakeRun(text[(i + 1)..close], forceBold, true, false, size));
                    i = close + 1;
                    continue;
                }
            }

            // `code`
            if (text[i] == '`')
            {
                int close = text.IndexOf('`', i + 1);
                if (close > i)
                {
                    FlushPlain();
                    inlines.Add(MakeRun(text[(i + 1)..close], forceBold, false, true, size));
                    i = close + 1;
                    continue;
                }
            }

            plain.Append(text[i]);
            i++;
        }

        FlushPlain();
    }

    private static Run MakeRun(string text, bool bold, bool italic, bool code, double? size)
    {
        var run = new Run(text);
        if (bold)
            run.FontWeight = FontWeight.SemiBold;
        if (italic)
            run.FontStyle = FontStyle.Italic;
        if (size is double s)
            run.FontSize = s;
        if (code)
        {
            run.FontFamily = new FontFamily("Cascadia Code,Consolas,Menlo,monospace");
            run.Foreground = new SolidColorBrush(Color.FromRgb(0xC0, 0x39, 0x2B));
        }
        return run;
    }
}
