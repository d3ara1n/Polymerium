using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Facilities;

namespace Polymerium.App.ViewModels;

public partial class PageNotReachedViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _message;

    public PageNotReachedViewModel(ViewBag bag)
    {
        if (bag.Parameter is string message)
            Message = message;
        else
            Message = "How about we explore the area ahead of us later?";
    }
}