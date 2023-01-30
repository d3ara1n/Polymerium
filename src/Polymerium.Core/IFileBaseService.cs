using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Polymerium.Abstractions;

namespace Polymerium.Core;

public interface IFileBaseService
{
    string Locate(Uri uri);

    bool TryReadAllText(Uri uri, out string text);

    void WriteAllText(Uri uri, string content);

    bool DoFileExist(Uri uri);

    Task<bool> VerfyHashAsync(Uri uri, string hash, HashAlgorithm algorithm);
    Task<Option<string>> ComputeHashAsync(Uri uri, HashAlgorithm algorithm);

    bool RemoveDirectory(Uri uri);
}