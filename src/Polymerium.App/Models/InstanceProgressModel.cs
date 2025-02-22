using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Facilities;
using Trident.Abstractions.Tasks;

namespace Polymerium.App.Models;

public partial class InstanceProgressModel : ModelBase
{
    #region Reactive Properties

    [ObservableProperty]
    private double? _progress;

    #endregion

    private TrackerBase? _tracker;

    public void UpdateSource(TrackerBase tracker)
    {
        if (_tracker is not null)
            Unsubscribe(_tracker);

        _tracker = tracker;
        Subscribe(tracker);
    }

    private void Unsubscribe(TrackerBase tracker) { }

    private void Subscribe(TrackerBase tracker) { }
}