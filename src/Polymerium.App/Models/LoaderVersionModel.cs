using System;
using Trident.Abstractions.Resources;

namespace Polymerium.App.Models
{
    public record LoaderVersionModel(string Identity, string Version, DateTimeOffset ReleasedAt, ReleaseType Type, bool Highlighted = false)
    {
    }
}