namespace Polymerium.Trident.Utilities;

public static class FileHelper
{
    public static string? PickExists(string home, Span<string> candidates)
    {
        foreach (var candidate in candidates)
        {
            var path = Path.Combine(home, candidate);
            if (File.Exists(path)) return path;
        }

        return null;
    }

    public static string? PickRandomly(string home, string pattern)
    {
        var files = Directory.GetFiles(home, pattern);
        if (files.Length == 0) return null;

        var index = Random.Shared.Next(files.Length);
        return files[index];
    }
}