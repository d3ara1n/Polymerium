using System.Collections.Generic;

namespace Polymerium.App.Models;

public class FileLogSourceModel : LogSourceModelBase
{
    #region Direct

    public required IReadOnlyList<ScrapModel> Logs { get; init; }

    #endregion
}
