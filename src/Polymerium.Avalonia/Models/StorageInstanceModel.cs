using Polymerium.Avalonia.Facilities;

namespace Polymerium.Avalonia.Models;

public class StorageInstanceModel(string key, string name, ulong size) : ModelBase
{
    #region Direct

    public string Key => key;
    public string Name => name;
    public ulong Size => size;

    #endregion
}
