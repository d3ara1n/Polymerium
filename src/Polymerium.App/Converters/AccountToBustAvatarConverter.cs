using System;
using Microsoft.UI.Xaml.Data;
using Polymerium.Abstractions.Accounts;

namespace Polymerium.App.Converters;

public class AccountToBustAvatarConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is IGameAccount account
            ? $"https://minotar.net/bust/{account.UUID}/100.png"
            : value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
