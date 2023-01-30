using System;

namespace Polymerium.Abstractions.Models.Game;

public class Library
{
    public string Name { get; set; }
    public string Path { get; set; }
    public string Sha1 { get; set; }
    public Uri Url { get; set; }
    public bool IsNative { get; set; }
}