namespace Polymerium.Trident.Helpers;

public static class FileNameHelper
{
    public const string INVALID_CHARACTERS = "\0\\/<>:\"|?*";

    public static string Sanitize(string fileName)
    {
        var output = fileName.Replace(' ', '_');
        foreach (var ch in INVALID_CHARACTERS) output = output.Replace(ch, '_');

        while (output.Contains("__")) output = output.Replace("__", "_");
        return output;
    }
}