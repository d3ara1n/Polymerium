using System.Collections.Generic;

namespace Polymerium.Avalonia.Models;

public abstract record LocalizedLabelBase
{
    public static LocalizedLabelBase Key(string key) => new KeyLabel(key);

    public static LocalizedLabelBase Literal(string value) => new LiteralLabel(value);

    public static LocalizedLabelBase Formatted(string key, params object?[] arguments) =>
        new FormattedLabel(key, arguments);

    public sealed record KeyLabel(string Value) : LocalizedLabelBase;

    public sealed record LiteralLabel(string Value) : LocalizedLabelBase;

    public sealed record FormattedLabel(string FormatKey, IReadOnlyList<object?> Arguments) : LocalizedLabelBase;
}
