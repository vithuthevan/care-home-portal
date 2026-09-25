using System.Net;
using System.Text.RegularExpressions;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace CareHome.Api.Documents;

internal static partial class RichTextPdf
{
    public static void Compose(IContainer container, string? html, float fontSize)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return;
        }

        var blocks = Blocks(html);
        container.Column(column =>
        {
            column.Spacing(2);
            foreach (var block in blocks)
            {
                if (string.IsNullOrWhiteSpace(block.Html))
                {
                    continue;
                }

                if (block.ListItem)
                {
                    column.Item().Row(row =>
                    {
                        row.ConstantItem(12).AlignTop().Text("•").FontSize(fontSize);
                        row.RelativeItem().Text(text => WriteInline(text, block.Html, fontSize));
                    });
                }
                else
                {
                    column.Item().Text(text => WriteInline(text, block.Html, fontSize));
                }
            }
        });
    }

    private static List<(bool ListItem, string Html)> Blocks(string html)
    {
        if (html.Contains("<li", StringComparison.OrdinalIgnoreCase))
        {
            return ListItemPattern().Matches(html)
                .Select(match => (true, match.Groups[1].Value))
                .ToList();
        }

        var normalized = BreakPattern().Replace(html, "\n");
        return normalized
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => (false, line))
            .ToList();
    }

    private static void WriteInline(TextDescriptor text, string html, float fontSize)
    {
        var bold = false;
        var italic = false;
        var underline = false;
        foreach (Match match in InlinePattern().Matches(html))
        {
            var token = match.Value;
            if (token.StartsWith('<'))
            {
                var closing = token.StartsWith("</", StringComparison.Ordinal);
                var name = match.Groups[1].Value.ToLowerInvariant();
                var enabled = !closing;
                switch (name)
                {
                    case "b":
                    case "strong":
                        bold = enabled;
                        break;
                    case "i":
                    case "em":
                        italic = enabled;
                        break;
                    case "u":
                        underline = enabled;
                        break;
                }

                continue;
            }

            var value = WebUtility.HtmlDecode(token);
            if (string.IsNullOrEmpty(value))
            {
                continue;
            }

            var span = text.Span(value).FontSize(fontSize);
            if (bold)
            {
                span.Bold();
            }

            if (italic)
            {
                span.Italic();
            }

            if (underline)
            {
                span.Underline();
            }
        }
    }

    [GeneratedRegex(@"<li[^>]*>(.*?)</li>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ListItemPattern();

    [GeneratedRegex(@"<br\s*/?>|</(?:p|div|li)>", RegexOptions.IgnoreCase)]
    private static partial Regex BreakPattern();

    [GeneratedRegex(@"</?(b|strong|i|em|u)\b[^>]*>|[^<]+", RegexOptions.IgnoreCase)]
    private static partial Regex InlinePattern();
}
