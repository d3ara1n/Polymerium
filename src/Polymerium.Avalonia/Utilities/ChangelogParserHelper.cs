using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Polymerium.Avalonia.Models;

namespace Polymerium.Avalonia.Utilities;

public static partial class ChangelogParserHelper
{
    public static ChangelogDocumentModel? Parse(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return null;
        }

        var document = Markdown.Parse(markdown);
        var entries = new Dictionary<ChangelogSectionKind, List<ChangelogEntryModel>>();
        ChangelogSectionKind? current = null;

        foreach (var block in document)
        {
            if (block is HeadingBlock heading)
            {
                current = heading.Level == 3 ? ProbeSection(heading) : null;
                continue;
            }

            if (current is not { } kind || block is not ListBlock { IsOrdered: false } list)
            {
                continue;
            }

            foreach (var child in list)
            {
                if (child is not ListItemBlock item || item.Count == 0)
                {
                    continue;
                }

                // NOTE: Rich block lists cannot be flattened without losing content; preserve the original document instead.
                if (item.Count != 1 || item[0] is not ParagraphBlock paragraph)
                {
                    return null;
                }

                var text = markdown.Substring(paragraph.Span.Start, paragraph.Span.Length).Trim();
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                if (!entries.TryGetValue(kind, out var section))
                {
                    section = [];
                    entries.Add(kind, section);
                }

                section.Add(kind == ChangelogSectionKind.Highlights
                                ? new(text, [])
                                : ParseEntry(text, paragraph));
            }
        }

        var sections = new List<ChangelogSectionModel>();
        foreach (var kind in new[]
                 {
                     ChangelogSectionKind.Fixed, ChangelogSectionKind.Added,
                     ChangelogSectionKind.Changed, ChangelogSectionKind.Removed,
                 })
        {
            if (entries.TryGetValue(kind, out var section) && section.Count > 0)
            {
                sections.Add(new(kind, section));
            }
        }

        return sections.Count == 0
                   ? null
                   : new(entries.GetValueOrDefault(ChangelogSectionKind.Highlights) ?? [], sections);
    }

    private static ChangelogSectionKind? ProbeSection(HeadingBlock heading)
    {
        if (heading.Inline?.FirstChild is not LiteralInline literal || literal.NextSibling is not null)
        {
            return null;
        }

        return literal.Content.ToString().Trim() switch
        {
            "Highlights" or "✨ Highlights ✨" => ChangelogSectionKind.Highlights,
            "Fixed" => ChangelogSectionKind.Fixed,
            "Added" => ChangelogSectionKind.Added,
            "Changed" => ChangelogSectionKind.Changed,
            "Removed" => ChangelogSectionKind.Removed,
            _ => null,
        };
    }

    private static ChangelogEntryModel ParseEntry(string markdown, ParagraphBlock paragraph)
    {
        // NOTE: Only plain trailing text can contain metadata; parentheses inside code or links belong to the prose.
        if (paragraph.Inline?.LastChild is not LiteralInline literal)
        {
            return new(markdown, []);
        }

        var match = TrailingReferences().Match(markdown);
        if (!match.Success || !literal.Content.ToString().TrimEnd().EndsWith(match.Value.TrimStart(), StringComparison.Ordinal))
        {
            return new(markdown, []);
        }

        var references = new List<ChangelogReferenceModel>();
        foreach (var value in match.Groups[1].Value.Split(',').Select(x => x.Trim()))
        {
            if (!ReferenceToken().IsMatch(value))
            {
                return new(markdown, []);
            }

            references.Add(new(value, GitHubIssue().IsMatch(value)
                                          ? new Uri($"https://github.com/d3ara1n/Polymerium/issues/{value[1..]}")
                                          : null));
        }

        var body = markdown[..match.Index].TrimEnd();
        return body.Length == 0 ? new(markdown, []) : new(body, references);
    }

    [GeneratedRegex(@"\s+\(([^()]+)\)$", RegexOptions.CultureInvariant)]
    private static partial Regex TrailingReferences();

    [GeneratedRegex(@"\A(?:#[1-9][0-9]*|#?POLY-[1-9][0-9]*|#?POLYMERIUM-[A-Z0-9]+|Huskui\.Avalonia(?:\.[A-Za-z0-9]+)*)\z", RegexOptions.CultureInvariant)]
    private static partial Regex ReferenceToken();

    [GeneratedRegex(@"\A#[1-9][0-9]*\z", RegexOptions.CultureInvariant)]
    private static partial Regex GitHubIssue();
}
