using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
        private readonly MainFileBaseOptions _options;
        private readonly MemoryStorage _memory;
        public MainFileBaseService(IOptions<MainFileBaseOptions> options, MemoryStorage memory)
        {
            _options = options.Value;
            _memory = memory;
        }

        public string Locate(Uri uri)
        {

            if (uri.IsAbsoluteUri)
            {
                if (uri.Scheme != "poly-file" && !string.IsNullOrEmpty(uri.Scheme))
                    throw new ArgumentException("Not valid poly-file url");
                if (string.IsNullOrEmpty(uri.Host))
                {
                    return Path.Combine(_options.BaseFolder, uri.GetComponents(UriComponents.Path, UriFormat.Unescaped));
                }
                else
                {
                    var instance = _memory.Instances.FirstOrDefault(x => x.Id == uri.Host);
                    if (instance != null)
                        return new Uri(new Uri(new Uri(new Uri(_options.BaseFolder), "instances/"), instance.FolderName + '/'), uri.GetComponents(UriComponents.Path, UriFormat.Unescaped)).LocalPath;
                    else
                        throw new ArgumentException("Instance id not presented in managed list");
                }
            }
            else
            {
                return new Uri(new Uri(_options.BaseFolder, UriKind.Absolute), uri).LocalPath;
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

        public bool DoFileExist(Uri uri) => File.Exists(Locate(uri));

        public async Task<bool> VerfyHashAsync(Uri uri, string hash, HashAlgorithm algorithm)
        {
            var path = Locate(uri);
            if (!File.Exists(path)) return false;
            using (var reader = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                var buffer = new byte[reader.Length];
                await reader.ReadAsync(buffer, 0, buffer.Length);
                var hashBytes = algorithm.ComputeHash(buffer);
                var hashString = String.Join(string.Empty, hashBytes.Select(x => x.ToString("x2")));
                return hash == hashString;
            }
        }

        public bool RemoveDirectory(Uri uri)
        {
            var path = Locate(uri);
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
