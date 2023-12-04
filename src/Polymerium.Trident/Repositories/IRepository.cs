using DotNext;
using Polymerium.Trident.Errors;

namespace Polymerium.Trident.Repositories;

public interface IRepository
{
    public class Filter(Optional<string> version, Optional<string> modLoader, Optional<Package.Kind> kind)
    {
        public Optional<string> Version => version;
        public Optional<string> ModLoader => modLoader;
        public Optional<Package.Kind> Kind => kind;

        public static Filter Empty { get; } =
            new Filter(Optional<string>.None, Optional<string>.None, Optional<Package.Kind>.None);
    }

    public bool Match(string identity);

    public void Search(string keyword, int page, int limit, Filter filter);

    public Result<Package, ResourceError> Resolve(string projectId, string? versionId, Filter filter);
}