using System;
using Irihi.Lingua;

namespace Polymerium.Avalonia;

public static class LinguaExtensions
{
    public static string Current(this IObservable<string?> observable) =>
        observable is LinguaObservable<string?> s ? s.CurrentValue ?? string.Empty : string.Empty;
}
