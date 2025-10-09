using System.Collections.ObjectModel;

namespace Polymerium.App.Models;

public class AssetScreenshotCollection : ObservableCollection<AssetScreenshotGroupModel>
{
    #region Reactive

    public int ScreenshotCount
    {
        get => field;
        set
        {
            field = value;
            OnPropertyChanged(new(nameof(ScreenshotCount)));
        }
    }

    #endregion
}
