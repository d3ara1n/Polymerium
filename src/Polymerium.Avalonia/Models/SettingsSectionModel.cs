using Avalonia.Controls;
using FluentIcons.Common;
using Polymerium.Avalonia.Facilities;

namespace Polymerium.Avalonia.Models;

public sealed class SettingsSectionModel: ModelBase
{
    #region Direct
    public required string Title { get; set; }

    public Symbol Icon { get; set; }

    public required Control Target { get; set; }
    #endregion
}
