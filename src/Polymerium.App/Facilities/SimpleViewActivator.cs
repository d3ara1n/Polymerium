using System;
using System.Text.RegularExpressions;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Mvvm.Activation;
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
            return ResolveViewModelType(view,nameof(Page));
        }
        else if (view.IsAssignableTo(typeof(Dialog)))
        {
            return ResolveViewModelType(view,nameof(Dialog));
        }
        else if (view.IsAssignableTo(typeof(Modal)))
        {
            return ResolveViewModelType(view,nameof(Modal));
        }
        else if (view.IsAssignableTo(typeof(Sidebar)))
        {
            return ResolveViewModelType(view,nameof(Sidebar));
        }
        else if (view.IsAssignableTo(typeof(Toast)))
        {
            return ResolveViewModelType(view,nameof(Toast));
        }

        throw new ArgumentOutOfRangeException(
            nameof(view),
            view,
            "Parameter view must be derived from Page/Dialog/Sidebar/Toast"
        );
    }

    private static Type ResolveViewModelType(Type view, string suffix)
    {
        var pattern = $@"\.{suffix}s\.|(?<=\w){suffix}$";
        var replaced = Regex.Replace(
                                     view.FullName!,
                                     pattern,
                                     m => m.Value.StartsWith('.') ? $".{suffix}Models." : $"{suffix}Model"
                                    );
        return Type.GetType(replaced)!;
    }
}
