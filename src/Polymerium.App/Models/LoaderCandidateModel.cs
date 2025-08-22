using Avalonia.Media.Imaging;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public class LoaderCandidateModel(string id, string display, Bitmap thumbnail) : ModelBase
{
    #region Direct

    public string Id => id;
    public string Display => display;
    public Bitmap Thumbnail => thumbnail;

    #endregion
}