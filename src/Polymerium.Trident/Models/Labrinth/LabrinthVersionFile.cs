namespace Polymerium.Trident.Models.Labrinth;

public struct LabrinthVersionFile
{
    public LabrinthVersionFileHash Hashes { get; set; }
    public Uri Url { get; set; }
    public string Filename { get; set; }
    public bool Primary { get; set; }
    public ulong Size { get; set; }
    public string FileType { get; set; }
}