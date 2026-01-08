using Avalonia.Media.Imaging;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public class RecentPlayScreenshotModel : ModelBase
{
    public required string FilePath { get; init; }
    public required Bitmap Image { get; init; }
}
