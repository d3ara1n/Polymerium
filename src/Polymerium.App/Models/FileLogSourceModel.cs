namespace Polymerium.App.Models;

public class FileLogSourceModel : LogSourceModelBase
{
    #region Direct

    public required string Path { get; init; }
    public string Name => System.IO.Path.GetFileName(Path);

    #endregion
}
