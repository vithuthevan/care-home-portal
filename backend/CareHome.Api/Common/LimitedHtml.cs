using System.Text.RegularExpressions;

namespace CareHome.Api.Common;

public static partial class LimitedHtml
{
    private static readonly HashSet<string> AllowedTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "b", "strong", "i", "em", "u", "br", "p", "div", "ul", "ol", "li"
    };

    public static string? Sanitize(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        var value = ScriptStylePattern().Replace(html, string.Empty);
        value = TagPattern().Replace(value, match =>
        {
            var name = match.Groups[1].Value;
            if (!AllowedTags.Contains(name))
            {
                return string.Empty;
            }

            if (name.Equals("br", StringComparison.OrdinalIgnoreCase))
            {
                return "<br>";
            }

            return match.Value.StartsWith("</", StringComparison.Ordinal)
                ? $"</{name.ToLowerInvariant()}>"
                : $"<{name.ToLowerInvariant()}>";
        });

        value = value.Trim();
        return value.Length == 0 ? null : value;
    }

    [GeneratedRegex(@"<(script|style)\b[^>]*>.*?</\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ScriptStylePattern();

    [GeneratedRegex(@"</?([a-zA-Z0-9]+)(?:\s[^>]*)?>", RegexOptions.IgnoreCase)]
    private static partial Regex TagPattern();
}
