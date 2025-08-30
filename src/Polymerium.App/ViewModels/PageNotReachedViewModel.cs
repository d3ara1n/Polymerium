using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Facilities;

namespace Polymerium.App.ViewModels;

public partial class PageNotReachedViewModel : ViewModelBase
{
    public PageNotReachedViewModel(ViewBag bag)
    {
        if (bag.Parameter is string message)
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