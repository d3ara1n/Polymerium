using Avalonia.Media.Imaging;
using Polymerium.Avalonia.Facilities;

namespace Polymerium.Avalonia.Models;

public class RecentPlayScreenshotModel : ModelBase
{
    public required string FilePath { get; init; }
    public required Bitmap Image { get; init; }
}
