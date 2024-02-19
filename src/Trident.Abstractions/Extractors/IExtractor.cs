using DotNext;
using Trident.Abstractions.Errors;

namespace Trident.Abstractions.Extractors
{
    public interface IExtractor
    {
        public string IdenticalFileName { get; }

        public Task<Result<ExtractedContainer, ExtractError>> ExtractAsync(string manifestContent,
            ExtractorContext context,
            CancellationToken token);
    }
}