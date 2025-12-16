using System.Globalization;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public class LanguageModel(CultureInfo info) : ModelBase
{
    #region Direct

    public string Id => info.Name;
    public string Display => info.DisplayName;

    #endregion

    public override int GetHashCode() => Id.GetHashCode();
    public override bool Equals(object? obj) => obj is LanguageModel other && other.Id == Id;
}
