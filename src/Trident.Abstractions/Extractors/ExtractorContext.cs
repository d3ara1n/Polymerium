using Trident.Abstractions.Resources;

namespace Trident.Abstractions.Extractors
{
    public record ExtractorContext((Project, Project.Version)? Source)
    {
    }
}