using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public class JavaInstallationModel(string path, string? vendor, string? version, uint? major) : ModelBase
{
    #region Direct

    public string Path => path;
    public string? Vendor => vendor;
    public string? Version => version;
    public uint? Major => major;

    #endregion
}