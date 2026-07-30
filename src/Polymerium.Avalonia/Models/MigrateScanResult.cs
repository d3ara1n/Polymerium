using System.Collections.ObjectModel;

namespace Polymerium.Avalonia.Models;

public class MigrateScanResult
{
    public required ObservableCollection<MigrateInstanceModel> Instances { get; init; }
}
