using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.UI.Xaml.Media;
using Polymerium.Core;

namespace Polymerium.App.Services
{
    // 确保文件父目录存在，能从 poly-file url 转换到具体目录
    public class MainFileBaseService : IFileBaseService
    {
        public readonly MainFileBaseOptions _options;
        public MainFileBaseService(IOptions<MainFileBaseOptions> options)
        {
            _options = options.Value;
        }

        // 只取 path 部分
        public string Locate(Uri uri)
        {

            if (uri.IsAbsoluteUri)
            {
                if (uri.Scheme != "poly-file" && !string.IsNullOrEmpty(uri.Scheme))
                    throw new ArgumentException("Not valid poly-file url");
                return new Uri(new Uri(_options.BaseFolder, UriKind.Absolute), uri.GetComponents(UriComponents.Path, UriFormat.Unescaped)).AbsolutePath;
            }
            else
            {
                return new Uri(new Uri(_options.BaseFolder, UriKind.Absolute), uri).AbsolutePath;
            }
        }

        public bool TryReadAllText(Uri uri, out string text)
        {
            var path = Locate(uri);
            if (File.Exists(path))
            {
                text = File.ReadAllText(path);
                return true;
            }
            else
            {
                text = default;
                return false;
            }
        }

        public void WriteAllText(Uri uri, string content)
        {
            var path = Locate(uri);
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            File.WriteAllText(path, content);
        }
    }
}
