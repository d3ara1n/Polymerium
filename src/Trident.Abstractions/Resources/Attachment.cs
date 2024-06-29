namespace Trident.Abstractions.Resources
{
    public record Attachment
    {
        public string Label { get; init; }
        public string ProjectId { get; init; }
        public string? VersionId { get; set; }

        public Attachment(string label, string projectId, string? versionId)
        {
            Label = label;
            ProjectId = projectId;
            VersionId = versionId;
        }

        public override string ToString()
        {
            return VersionId != null ? $"{Label}:{ProjectId}/{VersionId}" : $"{Label}:{ProjectId}/*";
        }
    }
}