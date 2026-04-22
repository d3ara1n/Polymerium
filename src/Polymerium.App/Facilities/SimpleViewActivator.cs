using System;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Mvvm.Activation;
using Huskui.Avalonia.Mvvm.Models;
using Huskui.Avalonia.Mvvm.States;
using Polymerium.App.Exceptions;
using Polymerium.App.Pages;

namespace Polymerium.App.Facilities;

public class SimpleViewActivator(IServiceProvider provider, IViewStateManager stateManager)
    : ViewActivatorBase(provider, stateManager)
{
    private static int activatorErrorCount;

    public override object? Activate(Type viewType, object? parameter = null)
    {
        try
        {
            var view = base.Activate(viewType, parameter);
            activatorErrorCount = 0;
            return view;
        }
        catch (NavigationFailedException ex)
        {
            // 避免又产生异常而导致无限循环
            if (activatorErrorCount++ < 3)
            {
                return Activate(typeof(PageNotReachedPage), ex.Message);
            }

            throw;
        }
        catch (Exception ex)
        {
            // 避免又产生异常而导致无限循环
            if (activatorErrorCount++ < 3)
            {
                return Activate(typeof(ExceptionPage), ex);
            }

            throw;
        }
    }

    protected override Type FindViewModelType(Type view)
    {
        if (view.IsAssignableTo(typeof(Page)))
        {
            return Type.GetType(view.FullName!.Replace("Page", "PageModel"))!;
        }
        else if (view.IsAssignableTo(typeof(Dialog)))
        {
            return Type.GetType(view.FullName!.Replace("Dialog", "DialogModel"))!;
        }
        else if (view.IsAssignableTo(typeof(Modal)))
        {
            return Type.GetType(view.FullName!.Replace("Modal", "ModalModel"))!;
        }
        else if (view.IsAssignableTo(typeof(Sidebar)))
        {
            return Type.GetType(view.FullName!.Replace("Sidebar", "SidebarModel"))!;
        }
        else if (view.IsAssignableTo(typeof(Toast)))
        {
            return Type.GetType(view.FullName!.Replace("Toast", "ToastModel"))!;
        }

        throw new ArgumentOutOfRangeException(
            nameof(view),
            view,
            "Parameter view must be derived from Page/Dialog/Sidebar/Toast"
        );
    }
}
