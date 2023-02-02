using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Polymerium.Abstractions;
using Polymerium.Core;

namespace Polymerium.App.Services;

// 确保文件父目录存在，能从 poly-file url 转换到具体目录
public class MainFileBaseService : IFileBaseService
{
    private readonly MemoryStorage _memory;
    private readonly MainFileBaseOptions _options;

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
                return new Uri(new Uri(_options.BaseFolder), uri.GetComponents(UriComponents.Path, UriFormat.Unescaped))
                    .LocalPath;

            var instance = _memory.Instances.FirstOrDefault(x => x.Id == uri.Host);
            if (instance != null)
                return new Uri(new Uri(new Uri(new Uri(_options.BaseFolder), "instances/"), instance.FolderName + '/'),
                    uri.GetComponents(UriComponents.Path, UriFormat.Unescaped)).LocalPath;
            throw new ArgumentException("Instance id not presented in managed list");
        }

        return new Uri(new Uri(_options.BaseFolder, UriKind.Absolute), uri).LocalPath;
    }

    public bool TryReadAllText(Uri uri, out string text)
    {
        var path = Locate(uri);
        if (File.Exists(path))
        {
            text = File.ReadAllText(path);
            return true;
        }

        text = default;
        return false;
    }

    public void WriteAllText(Uri uri, string content)
    {
        var path = Locate(uri);
        if (!Directory.Exists(Path.GetDirectoryName(path))) Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, content);
    }

    public bool DoFileExist(Uri uri)
    {
        return File.Exists(Locate(uri));
    }

    public async Task<bool> VerifyHashAsync(Uri uri, string hash, HashAlgorithm algorithm)
    {
        var option = await ComputeHashAsync(uri, algorithm);
        if (option.TryUnwrap(out var res))
            return hash == res;
        return false;
    }

    public async Task<Option<string>> ComputeHashAsync(Uri uri, HashAlgorithm algorithm)
    {
        var path = Locate(uri);
        if (!File.Exists(path)) return Option<string>.None();
        using (var reader = new FileStream(path, FileMode.Open, FileAccess.Read))
        {
            var buffer = new byte[reader.Length];
            await reader.ReadAsync(buffer, 0, buffer.Length);
            var hashBytes = algorithm.ComputeHash(buffer);
            var hashString = string.Join(string.Empty, hashBytes.Select(x => x.ToString("x2")));
            return Option<string>.Some(hashString);
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

        return false;
    }
}