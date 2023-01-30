namespace Polymerium.Core.Models.Mojang.Indexes;

public struct IndexLoggingItem
{
    public string Argument { get; set; }
    public IndexLoggingItemFile File { get; set; }
    public string Type { get; set; }
}