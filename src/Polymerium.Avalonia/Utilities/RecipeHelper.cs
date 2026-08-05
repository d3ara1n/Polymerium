using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using TridentCore.Abstractions.Utilities;

namespace Polymerium.Avalonia.Utilities;

public static class RecipeHelper
{
    private const int CurrentVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public sealed class RecipeDocument
    {
        public int Version { get; set; } = CurrentVersion;

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public List<Item> Items { get; set; } = [];

        public sealed class Item
        {
            public string Pref { get; set; } = string.Empty;

            public List<string> Tags { get; set; } = [];

            public string? Note { get; set; }
        }
    }

    public static string Serialize(RecipeDocument document) => JsonSerializer.Serialize(document, JsonOptions);

    public static bool TryDeserialize(string text, [NotNullWhen(true)] out RecipeDocument? document)
    {
        try
        {
            var doc = JsonSerializer.Deserialize<RecipeDocument>(text, JsonOptions);
            if (doc is null || doc.Version != CurrentVersion)
            {
                document = null;
                return false;
            }

            document = doc;
            return true;
        }
        catch (JsonException)
        {
            document = null;
            return false;
        }
    }

    public static RecipeDocument ToDocument(
        string name,
        string? description,
        IEnumerable<(string Label, string? Namespace, string ProjectId, IReadOnlyList<string> Tags, string? Note)> items)
    {
        var document = new RecipeDocument { Name = name, Description = description };
        foreach (var (label, ns, projectId, tags, note) in items)
        {
            document.Items.Add(new()
            {
                Pref = PackageHelper.ToPref(label, ns, projectId, null),
                Tags = [..tags],
                Note = note
            });
        }

        return document;
    }

    public static bool TryExtractIdentity(
        RecipeDocument.Item item,
        [NotNullWhen(true)] out string? label,
        out string? ns,
        [NotNullWhen(true)] out string? projectId)
    {
        label = null;
        ns = null;
        projectId = null;
        if (!PackageHelper.TryParse(item.Pref, out var id))
        {
            return false;
        }

        label = id.Repository;
        ns = id.Namespace;
        projectId = id.Identity;
        return true;
    }

    // NOTE: RecipeItem.Tags 在 DB 里以 JSON 字符串存储，解析失败容错为空集而非抛出
    public static IReadOnlyList<string> DeserializeTags(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<string[]>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
