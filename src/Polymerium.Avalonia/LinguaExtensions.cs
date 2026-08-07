using System;
using Irihi.Lingua;

namespace Polymerium.Avalonia;

public static class LinguaExtensions
{
    public static string Current(this IObservable<string?> observable) =>
        ((LinguaObservableString)observable).CurrentValue ?? string.Empty;
}
