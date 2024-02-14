namespace Polymerium.Trident.Helpers;

public static class FileNameHelper
{
    public static string Sanitize(string fileName)
    {
        var output = fileName.Replace(' ', '_');
        foreach (var ch in Path.GetInvalidFileNameChars()) output = output.Replace(ch, '_');

        while (output.Contains("__")) output = output.Replace("__", "_");
        return output;
    }
}