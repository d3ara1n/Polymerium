using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public class AppUpdateModel(uint major, uint minor, uint patch, string? metadata) : ModelBase
{
    public string Version => $"{major}.{minor}.{patch}{(metadata is not null ? $"-{metadata}" : string.Empty)}";
}