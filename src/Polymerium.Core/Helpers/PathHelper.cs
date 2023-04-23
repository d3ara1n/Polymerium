using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Core.Helpers
{
    public static class PathHelper
    {
        private static readonly char[] invalidChars = Path.GetInvalidFileNameChars();

        public static string RemoveInvalidCharacters(string path) =>
            string.Join("", path.Select(x => invalidChars.Contains(x) ? '_' : x));
    }
}
