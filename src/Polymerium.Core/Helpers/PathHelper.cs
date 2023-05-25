using System.IO;
using System.Linq;

namespace Polymerium.Core.Helpers;

public static class PathHelper
{
    private static readonly char[] invalidChars = Path.GetInvalidFileNameChars();

    public static string RemoveInvalidCharacters(string path)
    {
        return string.Join("", path.Select(x => invalidChars.Contains(x) ? '_' : x));
    }
}
