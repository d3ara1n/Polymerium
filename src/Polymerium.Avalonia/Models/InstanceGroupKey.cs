using System;

namespace Polymerium.Avalonia.Models;

public sealed class InstanceGroupKey(string identity, LocalizedLabelBase label) : IEquatable<InstanceGroupKey>
{
    public string Identity { get; } = identity;

    public LocalizedLabelBase Label { get; } = label;

    public bool Equals(InstanceGroupKey? other) =>
        other is not null && string.Equals(Identity, other.Identity, StringComparison.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as InstanceGroupKey);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Identity);
}
