using System;
using Polymerium.App.Facilities;

namespace Polymerium.App.Models;

public class ImportBrowserEntryModel : ModelBase
{
    public required string Name { get; set; }

    public required string RelativePath { get; set; }

    public required string FullPath { get; set; }

    public required bool IsDirectory { get; set; }

    public required string FileType { get; set; }

    public required long FileSizeRaw { get; set; }

    public required DateTime FileLastModifiedRaw { get; set; }
}
