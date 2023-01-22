// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using ColorCode.Compilation.Languages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Polymerium.Abstractions;
using Polymerium.App.Controls;
using Polymerium.App.Services;
using Polymerium.App.ViewModels;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Polymerium.App.Views
{
    public sealed partial class PrepareGameDialog : ContentControl
    {
        public PrepareGameViewModel ViewModel { get; private set; }
        private readonly IOverlayService _overlayService;
        public PrepareGameDialog(GameInstance instance, IOverlayService overlayService)
        {
            this.InitializeComponent();
            _overlayService = overlayService;
            ViewModel = App.Current.Provider.GetRequiredService<PrepareGameViewModel>();
            ViewModel.GotInstance(instance, () => IsReady = true);
        }



        public bool IsReady
        {
            get { return (bool)GetValue(IsReadyProperty); }
            set { SetValue(IsReadyProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IsReady.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsReadyProperty =
            DependencyProperty.Register(nameof(IsReady), typeof(bool), typeof(PrepareGameDialog), new PropertyMetadata(false, new PropertyChangedCallback(IsReadyChanged)));

        private Border _layout;
        private ScaleTransform _scaleTransform;
        private Image _backgroundImage;
        private Storyboard fadeAnimation;
        private Storyboard scaleXAnimation;
        private Storyboard scaleYAnimation;
        protected override void OnApplyTemplate()
        {
            _layout = GetTemplateChild("Layout") as Border;
            _scaleTransform = GetTemplateChild("ScaleTransform") as ScaleTransform;
            _backgroundImage = GetTemplateChild("BackgroundImage") as Image;

            scaleXAnimation = new Storyboard();
            Storyboard.SetTarget(scaleXAnimation, _scaleTransform);
            Storyboard.SetTargetProperty(scaleXAnimation, "ScaleX");
            scaleXAnimation.Children.Add(new DoubleAnimation()
            {
                Duration = new Duration(TimeSpan.FromMilliseconds(150)),
                From = 1.05,
                To = 1.0
            });
            scaleYAnimation = new Storyboard();
            Storyboard.SetTarget(scaleYAnimation, _scaleTransform);
            Storyboard.SetTargetProperty(scaleYAnimation, "ScaleY");
            scaleYAnimation.Children.Add(new DoubleAnimation()
            {
                Duration = new Duration(TimeSpan.FromMilliseconds(150)),
                From = 1.05,
                To = 1.0
            });
            fadeAnimation = new Storyboard();
            Storyboard.SetTarget(fadeAnimation, _layout);
            Storyboard.SetTargetProperty(fadeAnimation, "Opacity");
            fadeAnimation.Children.Add(new DoubleAnimation()
            {
                Duration = new Duration(TimeSpan.FromMilliseconds(150)),
                From = 0,
                To = 1.0
            });
            base.OnApplyTemplate();
        }

        private void BackgroundImage_ImageOpened(object sender, RoutedEventArgs e)
        {
            scaleXAnimation.Begin();
            scaleYAnimation.Begin();
            fadeAnimation.Begin();
            Task.Run(ViewModel.PrepareAsync);
        }

        private void BackgroundImage_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            _backgroundImage.Source = new BitmapImage()
            {
                UriSource = new Uri("ms-appx:///Assets/Placeholders/default_panorama1.png")
            };
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var xAnimation = scaleXAnimation.Children[0] as DoubleAnimation;
            xAnimation.From = 1.0;
            xAnimation.To = 1.05;
            var yAnimation = scaleYAnimation.Children[0] as DoubleAnimation;
            yAnimation.From = 1.0;
            yAnimation.To = 1.05;
            var fAnimation = fadeAnimation.Children[0] as DoubleAnimation;
            fAnimation.From = 1.0;
            fAnimation.To = 0.0;
            fadeAnimation.Completed += FadeAnimation_Completed;
            scaleXAnimation.Begin();
            scaleYAnimation.Begin();
            fadeAnimation.Begin();
        }

        private void FadeAnimation_Completed(object sender, object e)
        {
            ViewModel.Cancel();
            _overlayService.Dismiss();
        }

        private static void IsReadyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            var dialog = sender as PrepareGameDialog;
            if (dialog.IsReady)
            {
                VisualStateManager.GoToState(dialog, "Ready", true);
            }
            else
            {
                VisualStateManager.GoToState(dialog, "Unready", true);
            }
        }
    }
}
