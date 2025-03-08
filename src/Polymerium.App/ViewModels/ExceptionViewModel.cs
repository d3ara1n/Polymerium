using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Facilities;

namespace Polymerium.App.ViewModels;

public partial class ExceptionViewModel : ViewModelBase
{
    public ExceptionViewModel(ViewBag bag)
    {
        if (bag.Parameter is Exception exception)
        {
            Message = exception.Message;
            StackTrace = exception.StackTrace ?? "No details provided.";
        }
    }

    [ObservableProperty]
    public partial string Message { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string StackTrace { get; set; } = string.Empty;
}