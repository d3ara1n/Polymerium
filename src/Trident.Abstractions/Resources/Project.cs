namespace Trident.Abstractions.Resources
{

    // 用于表示市场中的一个项目，仅用于展示
    // 从 IRepository.Query(string projectId) 获得

    public record Project(string ProjectId, string ProjectName, Uri? Thumbnail, string Author, string Summary, Uri Reference, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, uint DownloadCount, string DescriptionHtml, IEnumerable<Project.Screenshot> Gallery,
        IEnumerable<Project.Version> Versions)
    {
        public record Screenshot(string Title, Uri Url);

        public record Version(string ChangelogHtml, DateTimeOffset PublishedAt, IEnumerable<Dependency> Dependencies) { }
    }
}
