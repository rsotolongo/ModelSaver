using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ModelSaver.Internal;

internal static class RenderedNameScanner
{
    private static readonly Regex NamedElementPattern = new Regex(
        @"<(input|select|textarea)\b[^>]*>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex AttributePattern = new Regex(
        @"(\w[\w-]*)\s*=\s*(""([^""]*)""|'([^']*)')",
        RegexOptions.Compiled);

    private static readonly HashSet<string> ExcludedInputTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "submit", "button", "reset", "image"
    };

    public static HashSet<string> ExtractNames(string html)
    {
        if (html is null)
        {
            throw new ArgumentNullException(nameof(html));
        }

        var names = new HashSet<string>(StringComparer.Ordinal);

        foreach (Match element in NamedElementPattern.Matches(html))
        {
            var attributes = ParseAttributes(element.Value);

            if (attributes.TryGetValue("type", out var type) && ExcludedInputTypes.Contains(type))
            {
                continue;
            }

            if (attributes.TryGetValue("name", out var name) && name.Length > 0)
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
            var attributeName = match.Groups[1].Value;
            var value = match.Groups[3].Success ? match.Groups[3].Value : match.Groups[4].Value;
            attributes[attributeName] = value;
        }

        return attributes;
    }
}
