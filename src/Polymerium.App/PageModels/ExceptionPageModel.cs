using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Huskui.Avalonia.Mvvm.Activation;
using Huskui.Avalonia.Mvvm.Models;
using Polymerium.App.Facilities;

namespace Polymerium.App.PageModels;

public partial class ExceptionPageModel : ViewModelBase
{
    public ExceptionPageModel(IViewContext<Exception> context)
    {
        Message = context.GetRequiredParameter().Message;
        StackTrace = context.GetRequiredParameter().StackTrace ?? "No details provided.";
    }

    #region Reactive

    [ObservableProperty]
    public partial string Message { get; set; }

    [ObservableProperty]
    public partial string StackTrace { get; set; }

    #endregion
}
