// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Polymerium.App.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Polymerium.App.Controls;

public class CustomDialog : ContentControl
{
    public static readonly DependencyProperty OperationContentProperty = DependencyProperty.Register(
        nameof(OperationContent),
        typeof(object),
        typeof(CustomDialog),
        new PropertyMetadata(default, OperationContentPropertyChangedCallback)
    );

    public object OperationContent
    {
        get => GetValue(OperationContentProperty);
        set => SetValue(OperationContentProperty, value);
    }

    private static void OperationContentPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var dialog = d as CustomDialog;
        if (dialog?.OperationContent != null)
        {
            // 也可以 useTransition，但是 OperationContent 运行时不会改变，动画(没加)也不会出现，所以没啥意义
            VisualStateManager.GoToState(dialog, "WithOperation", false);
        }
        else
        {
            VisualStateManager.GoToState(dialog, "DefaultOperation", false);
        }
    }

    public static readonly DependencyProperty OperationPaddingProperty = DependencyProperty.Register(
        nameof(OperationPadding),
        typeof(Thickness),
        typeof(CustomDialog),
        new PropertyMetadata(default)
    );

    public Thickness OperationPadding
    {
        get => (Thickness)GetValue(OperationPaddingProperty);
        set => SetValue(OperationPaddingProperty, value);
    }

    private Grid _root;
    private ScaleTransform _borderScaleTransform;
    private Storyboard fadeoutStoryboard;
    private Storyboard scaleXOutStoryboard;
    private Storyboard scaleYOutStoryboard;

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        // 动画全部用代码实现，WPF 时期的前后端分离一去不复返
        _root = GetTemplateChild("Root") as Grid;
        _borderScaleTransform = GetTemplateChild("BorderScaleTransform") as ScaleTransform;
        Storyboard.SetTarget(fadeoutStoryboard, _root);
        Storyboard.SetTargetProperty(fadeoutStoryboard, "Opacity");
        Storyboard.SetTarget(scaleXOutStoryboard, _borderScaleTransform);
        Storyboard.SetTargetProperty(scaleXOutStoryboard, "ScaleX");
        Storyboard.SetTarget(scaleYOutStoryboard, _borderScaleTransform);
        Storyboard.SetTargetProperty(scaleYOutStoryboard, "ScaleY");
        VisualStateManager.GoToState(this, "DialogShown", true);
        if (OperationContent != null)
        {
            VisualStateManager.GoToState(this, "WithOperation", false);
        }
        else
        {
            VisualStateManager.GoToState(this, "DefaultOperation", false);
        }
    }

    public CustomDialog()
    {
        DefaultStyleKey = typeof(CustomDialog);

        fadeoutStoryboard = new Storyboard();
        fadeoutStoryboard.Children.Add(new DoubleAnimation()
        {
            Duration = new Duration(TimeSpan.FromMilliseconds(150)),
            To = 0.0
        });
        scaleXOutStoryboard = new Storyboard();
        scaleXOutStoryboard.Children.Add(new DoubleAnimation()
        {
            Duration = new Duration(TimeSpan.FromMilliseconds(150)),
            To = 1.05
        });
        scaleYOutStoryboard = new Storyboard();
        scaleYOutStoryboard.Children.Add(new DoubleAnimation()
        {
            Duration = new Duration(TimeSpan.FromMilliseconds(150)),
            To = 1.05
        });
        fadeoutStoryboard.Completed += (_, _) => OverlayService!.Dismiss();
    }

    public IOverlayService OverlayService { get; set; }

    public void Dismiss()
    {
        if (OverlayService != null)
        {
            //// 先触发动画，等动画结束触发事件之后调用服务里的 Dismiss()
            //if (VisualStateManager.GoToState(this, "DialogHidden", true))
            //{
            //    // 目前没法实现，直接杀了吧
            //    OverlayService.Dismiss();
            //}
            fadeoutStoryboard.Begin();
            scaleXOutStoryboard.Begin();
            scaleYOutStoryboard.Begin();
        }
        else
        {
            throw new ArgumentNullException(nameof(OverlayService));
        }
    }
}
