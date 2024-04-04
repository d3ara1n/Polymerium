namespace Trident.Abstractions.Extractors
{
    public interface IExtractor
    {
        public string IdenticalFileName { get; }

        public Task<ExtractedContainer> ExtractAsync(string manifestContent,
            ExtractorContext context,
            CancellationToken token);
    }
}