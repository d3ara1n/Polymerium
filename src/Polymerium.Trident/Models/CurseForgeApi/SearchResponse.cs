namespace Polymerium.Trident.Models.CurseForgeApi;

public record SearchResponse<T>(IReadOnlyList<T> Data, SearchResponse<T>.Page Pagination)
{
    #region Nested type: Page

    public record Page(uint Index, uint PageSize, uint ResultCount, uint TotalCount);

    #endregion
}