using System;
using Polymerium.Abstractions;

namespace Polymerium.App.Services;

public enum LocalRepositoryError
{
}

public class LocalRepositoryService
{
    public Result<Uri, LocalRepositoryError> AllocInstanceLink(string instanceId, string path)
    {
        return Result<Uri, LocalRepositoryError>.Ok(new Uri(new Uri($"poly-file:///local/instances/{instanceId}/"),
            path));
    }
}