using System.Text.RegularExpressions;

namespace ProQualEx.ModelSaver.Internal;

internal static class RenderedNameScanner
{
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);

    private static readonly Regex NamedElementPattern = new(
        @"<(input|select|textarea)\b[^>]*>",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase,
        MatchTimeout);

    private static readonly Regex AttributePattern = new(
        @"(?<name>\w[\w-]*)\s*=\s*(?:""(?<double>[^""]*)""|'(?<single>[^']*)')",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture,
        MatchTimeout);

    private static readonly HashSet<string> ExcludedInputTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "submit",
        "button",
        "reset",
        "image",
    };

    internal static HashSet<string> ExtractNames(string html)
    {
        if (html is null)
        {
            throw new ArgumentNullException(nameof(html));
        }

        var names = new HashSet<string>(StringComparer.Ordinal);

        foreach (Match element in NamedElementPattern.Matches(html))
        {
            Dictionary<string, string> attributes = ParseAttributes(element.Value);

            if (attributes.TryGetValue("type", out string? type) && ExcludedInputTypes.Contains(type))
            {
                continue;
            }

            if (attributes.TryGetValue("name", out string? name) && name.Length > 0)
            {
                names.Add(name);
            }
        }

        return names;
    }

    private static Dictionary<string, string> ParseAttributes(string elementHtml)
    {
        var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in AttributePattern.Matches(elementHtml))
        {
            string attributeName = match.Groups["name"].Value;
            attributes[attributeName] = match.Groups["double"].Success
                ? match.Groups["double"].Value
                : match.Groups["single"].Value;
        }

        return attributes;
    }
}
