// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Polymerium.App.Controls;
using Polymerium.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Polymerium.App.Models;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Polymerium.App.Views
{
    public sealed partial class InstanceUpdateDialog : CustomDialog
    {
        public InstanceUpdateDialog(
            GameInstanceModel instance,
            InstanceModpackReferenceModel modpack,
            InstanceModpackReferenceVersionModel version
        )
        {
            ViewModel = App.Current.Provider.GetRequiredService<InstanceUpdateViewModel>();
            this.InitializeComponent();
        }

        public InstanceUpdateViewModel ViewModel { get; }
    }
}
