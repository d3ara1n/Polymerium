namespace Polymerium.Trident.Helpers;

public static class FileNameHelper
{
    public const string INVALID_CHARACTERS = "\\/<>:\"|?*" +
                                             "\u0000\u0001\u0002\u0003\u0004\u0005\u0006\u0007" +
                                             "\u0008\u0009\u0010\u0011\u0012\u0013\u0014\u0015" +
                                             "\u0016\u0017\u0018\u0019\u0020\u0021\u0022\u0023" +
                                             "\u0024\u0025\u0026\u0027\u0028\u0029\u0030\u0031";

    public static string Sanitize(string fileName)
    {
        var output = fileName.Replace(' ', '_');
        foreach (var ch in INVALID_CHARACTERS) output = output.Replace(ch, '_');

        while (output.Contains("__")) output = output.Replace("__", "_");
        return output;
    }
}