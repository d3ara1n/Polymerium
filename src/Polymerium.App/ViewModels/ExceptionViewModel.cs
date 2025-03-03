using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Facilities;
using System;

namespace Polymerium.App.ViewModels;

public partial class ExceptionViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _message = string.Empty;

    [ObservableProperty]
    private string _stackTrace = string.Empty;

    public ExceptionViewModel(ViewBag bag)
    {
        if (bag.Parameter is Exception exception)
        {
            Message = exception.Message;
            StackTrace = exception.StackTrace ?? "No details provided.";
        }
    }
}