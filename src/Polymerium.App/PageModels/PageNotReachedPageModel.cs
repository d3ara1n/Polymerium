using CommunityToolkit.Mvvm.ComponentModel;
using Huskui.Avalonia.Mvvm.Activation;
using Huskui.Avalonia.Mvvm.Models;
using Polymerium.App.Facilities;

namespace Polymerium.App.PageModels;

public partial class PageNotReachedPageModel : ViewModelBase
{
    public PageNotReachedPageModel(IViewContext<string> context)
    {
        if (context.Parameter is { } message)
        {
            Message = message;
        }
        else
        {
            Message = "How about we explore the area ahead of us later?";
        }
    }

    #region Reactive

    [ObservableProperty]
    public partial string Message { get; set; }

    #endregion
}
