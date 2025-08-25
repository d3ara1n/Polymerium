namespace Polymerium.Trident.Models.ModrinthApi
{
    public readonly record struct ProjectInfo(
        string Id,
        string Slug,
        IReadOnlyList<string> ProjectTypes,
        IReadOnlyList<string> Games,
        string TeamId,
        string? Organization,
        string Name,
        string Summary,
        string Description,
        DateTimeOffset Published,
        DateTimeOffset Updated,
        DateTimeOffset? Approved,
        DateTimeOffset? Queued,
        string Status,
        string? RequestedStatus,
        string? ModeratorMessage,
        ProjectInfo.ProjectLicense License,
        ulong Downloads,
        uint Followers,
        IReadOnlyList<string> Categories,
        IReadOnlyList<string> AdditionalCategories,
        IReadOnlyList<string> Versions,
        Uri? IconUrl,
        ProjectInfo.ProjectLinks LinkUrls,
        IReadOnlyList<ProjectInfo.ProjectScreenshot> Gallery,
        long? Color,
        string ThreadId,
        string MonetizationStatus,
        IReadOnlyList<bool> ClientAndServer,
        IReadOnlyList<bool> Singleplayer,
        IReadOnlyList<bool> ClientOnly,
        IReadOnlyList<string> GameVersions,
        IReadOnlyList<string> MrpackLoaders,
        IReadOnlyList<bool> ServerOnly)
    {
        #region Nested type: ProjectLicense

        public readonly record struct ProjectLicense(string Id, string Name, Uri? Url);

        #endregion

        #region Nested type: ProjectLinks

        public readonly record struct ProjectLinks(
            ProjectLinks.Links? Other,
            ProjectLinks.Links? Discord,
            ProjectLinks.Links? Matrix,
            ProjectLinks.Links? Source,
            ProjectLinks.Links? Wiki,
            ProjectLinks.Links? Issues,
            ProjectLinks.Links? Website)
        {
            #region Nested type: Links

            public readonly record struct Links(string Platform, bool Donation, Uri Url);

            #endregion
        }

        #endregion

        #region Nested type: ProjectScreenshot

        public readonly record struct ProjectScreenshot(
            Uri Url,
            Uri RawUrl,
            bool Featured,
            string Name,
            string? Description,
            DateTimeOffset Created,
            int Ordering);

        #endregion
    }
}
