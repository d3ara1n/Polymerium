using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Polymerium.Core;

public interface IFileBaseService
{
    string Locate(Uri uri);

    bool TryReadAllText(Uri uri, out string text);

    void WriteAllText(Uri uri, string content);

    bool DoFileExist(Uri uri);

    Task<bool> VerifyHashAsync(Uri uri, string? hash, HashAlgorithm algorithm);
    Task<string?> ComputeHashAsync(Uri uri, HashAlgorithm algorithm);

    bool RemoveDirectory(Uri uri);

    Task<bool> CheckIfTheSameAsync(Uri left, Uri right);
}