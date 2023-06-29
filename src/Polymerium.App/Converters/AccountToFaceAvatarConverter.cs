using Microsoft.UI.Xaml.Data;
using Polymerium.Abstractions.Accounts;
using System;

namespace Polymerium.App.Converters;

public class AccountToFaceAvatarConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is IGameAccount account
            ? $"https://minotar.net/helm/{account.UUID}/100.png"
            : value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
