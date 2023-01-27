using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Polymerium.Core
{
    public interface IFileBaseService
    {
        string Locate(Uri uri);
        bool TryReadAllText(Uri uri, out string text);
        void WriteAllText(Uri uri, string content);
        bool DoFileExist(Uri uri);
        Task<bool> VerfyHashAsync(Uri uri, string hash, HashAlgorithm algorithm);
        bool RemoveDirectory(Uri uri);
    }
}
