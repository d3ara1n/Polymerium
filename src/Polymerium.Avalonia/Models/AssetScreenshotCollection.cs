using System.Collections.ObjectModel;

namespace Polymerium.Avalonia.Models;

public class AssetScreenshotCollection : ObservableCollection<AssetScreenshotGroupModel>
{
    #region Reactive

    public int ScreenshotCount
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(new(nameof(ScreenshotCount)));
        }
    }

    #endregion
}
