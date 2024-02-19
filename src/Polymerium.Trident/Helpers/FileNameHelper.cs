namespace Polymerium.Trident.Helpers
{
    public static class FileNameHelper
    {
        public static string Sanitize(string fileName)
        {
            string output = fileName.Replace(' ', '_').Replace('-', '_').Replace('.', '_');
            foreach (char ch in Path.GetInvalidFileNameChars())
            {
                output = output.Replace(ch, '_');
            }

            while (output.Contains("__"))
            {
                output = output.Replace("__", "_");
            }

            return output;
        }
    }
}