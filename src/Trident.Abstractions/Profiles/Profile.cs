namespace Trident.Abstractions.Profiles;

// 游戏本身需要传入 Metadata,IDictionary<string, object> Setup+Overrides, Account.Token
public record Profile(
    string Name,
    Uri? Thumbnail,
    string? Reference,
    RecordData Records,
    Metadata Metadata,
    IDictionary<string, object> Overrides,
    string? AccountId)
{
    public string Name { get; set; } = Name;
    public Uri? Thumbnail { get; set; } = Thumbnail;
    public string? Reference { get; set; } = Reference;
    public string? AccountId { get; set; } = AccountId;
}