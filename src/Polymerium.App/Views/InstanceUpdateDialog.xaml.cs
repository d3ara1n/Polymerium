// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.DependencyInjection;
using Polymerium.App.Controls;
using Polymerium.App.Models;
using Polymerium.App.ViewModels;

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

        private void CancelButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            Dismiss();
        }
    }
}
