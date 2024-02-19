using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Polymerium.App.Dialogs;
using System;
using System.Threading.Tasks;

namespace Polymerium.App.Services
{
    public class DialogService
    {
        private XamlRoot XamlRoot
        {
            get
            {
                XamlRoot? xamlRoot = App.Current.Window.Content.XamlRoot;
                ArgumentNullException.ThrowIfNull(xamlRoot);
                return xamlRoot;
            }
        }

        public async Task<string?> RequestTextAsync(string message, string defaultValue)
        {
            InputDialog dialog = new(XamlRoot) { Message = message, Placeholder = defaultValue };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                return dialog.Result;
            }

            return null;
        }

        public async Task<bool> RequestConfirmationAsync(string message)
        {
            ConfirmDialog dialog = new(XamlRoot) { Message = message };
            return await dialog.ShowAsync() == ContentDialogResult.Primary;
        }
    }
}