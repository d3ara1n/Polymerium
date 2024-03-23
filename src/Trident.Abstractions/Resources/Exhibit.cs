namespace Trident.Abstractions.Resources
{
    // 搜索结果，用与展示，仅包含表面数据
    // IRepository.Search(string keyword, int page, int limit, Filter filter) 获得

    public record Exhibit(
        string Id,
        string Name,
        string Label,
        Uri? Thumbnail,
        ResourceKind Kind,
        string Author,
        string Summary,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        uint DownloadCount)
    {
    }
}