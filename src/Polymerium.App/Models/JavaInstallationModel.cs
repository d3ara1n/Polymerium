using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public class JavaInstallationModel(string path, string? vendor, string? version, int? major) : ModelBase
{
    #region Direct

    public string Path => path;
    public string? Vendor => vendor;
    public string? Version => version;
    public int? Major => major;

    #endregion
}