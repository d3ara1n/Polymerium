using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Mvvm.Activation;
using Huskui.Avalonia.Mvvm.States;
using Polymerium.Avalonia.Exceptions;
using Polymerium.Avalonia.Pages;

namespace Polymerium.Avalonia.Facilities;

public class SimpleViewActivator(IServiceProvider provider, IViewStateManager stateManager)
    : ViewActivatorBase(provider, stateManager)
{
    private static int activatorErrorCount;

    public override object? Activate(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        Type viewType,
        object? parameter = null)
    {
        try
        {
            var view = base.Activate(viewType, parameter);
            activatorErrorCount = 0;
            return view;
        }
        catch (NavigationFailedException ex)
        {
            // NOTE: 避免再抛异常造成无限循环。
            if (activatorErrorCount++ < 3)
            {
                return Activate(typeof(PageNotReachedPage), ex.Message);
            }

            throw;
        }
        catch (Exception ex)
        {
            // NOTE: 避免再抛异常造成无限循环。
            if (activatorErrorCount++ < 3)
            {
                return Activate(typeof(ExceptionPage), ex);
            }

            throw;
        }
    }

    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    protected override Type FindViewModelType(Type view)
    {
        if (view.IsAssignableTo(typeof(Page)))
        {
            return ResolveViewModelType(view, nameof(Page));
        }

        if (view.IsAssignableTo(typeof(Dialog)))
        {
            return ResolveViewModelType(view, nameof(Dialog));
        }

        if (view.IsAssignableTo(typeof(Modal)))
        {
            return ResolveViewModelType(view, nameof(Modal));
        }

        if (view.IsAssignableTo(typeof(Sidebar)))
        {
            return ResolveViewModelType(view, nameof(Sidebar));
        }

        if (view.IsAssignableTo(typeof(Toast)))
        {
            return ResolveViewModelType(view, nameof(Toast));
        }

        throw new ArgumentOutOfRangeException(nameof(view),
                                              view,
                                              "Parameter view must be derived from Page/Dialog/Sidebar/Toast");
    }

    // NOTE: 类型名由 Regex 从视图全名动态拼接而来，trimmer 无法静态追踪；partial 模式下 app 程序集
    // 整体保留、入口全部是 Navigate/PopXxx 泛型调用点，故安全。full trim/AOT 前需重构为显式注册表。
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2057",
        Justification = "View types are rooted by the untrimmed app assembly under partial trim")]
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    private static Type ResolveViewModelType(Type view, string suffix)
    {
        var pattern = $@"\.{suffix}s\.|(?<=\w){suffix}$";
        var replaced = Regex.Replace(view.FullName!,
                                     pattern,
                                     m => m.Value.StartsWith('.') ? $".{suffix}Models." : $"{suffix}Model");
        return Type.GetType(replaced)!;
    }
}
