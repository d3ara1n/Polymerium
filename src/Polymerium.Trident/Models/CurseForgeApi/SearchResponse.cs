namespace Polymerium.Trident.Models.CurseForgeApi;

public readonly record struct SearchResponse<T>(IReadOnlyList<T> Data, SearchResponse<T>.Page Pagination)
{
    #region Nested type: Page

    public readonly record struct Page(uint Index, uint PageSize, uint ResultCount, uint TotalCount);

    #endregion
}