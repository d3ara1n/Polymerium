using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Polymerium.Avalonia.Models;

// Carries the scan results plus the commands the selection-phase view needs. The view model hands its
// own commands in when creating the result so the DataTemplate (DataContext = this object) can bind
// them directly without reaching back across the PlaceholderContainer boundary.
public class MigrateScanResult
{
    public required ObservableCollection<MigrateInstanceRow> Instances { get; init; }
    public required ICommand MigrateCommand { get; init; }
    public required ICommand BackCommand { get; init; }
}
