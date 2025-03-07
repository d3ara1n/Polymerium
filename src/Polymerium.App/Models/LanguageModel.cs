using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public class LanguageModel(string id, string display) : ModelBase
{
    #region Direct

    public string Id => id;
    public string Display => display;

    #endregion
}