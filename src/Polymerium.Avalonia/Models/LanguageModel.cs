using System.Globalization;
using Polymerium.Avalonia.Facilities;

namespace Polymerium.Avalonia.Models;

public class LanguageModel(CultureInfo info) : ModelBase
{
    public override int GetHashCode() => Id.GetHashCode();

    public override bool Equals(object? obj) => obj is LanguageModel other && other.Id == Id;

    #region Direct

    public string Id => info.Name;
    public string Display => info.DisplayName;

    #endregion
}
