using System;

namespace Polymerium.Abstractions.Models.Game;

public class Library
{
    public Library(string name, string path, string? sha1, Uri? url, bool isNative = false)
    {
        Name = name;
        Path = path;
        Sha1 = sha1;
        Url = url;
        IsNative = isNative;
    }

    public string Name { get; set; }
    public string Path { get; set; }
    public string? Sha1 { get; set; }
    public Uri? Url { get; set; }
    public bool IsNative { get; set; }
}