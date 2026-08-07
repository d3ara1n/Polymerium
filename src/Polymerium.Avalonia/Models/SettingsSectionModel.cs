using FluentIcons.Common;
using Polymerium.Avalonia.Controls;
using Polymerium.Avalonia.Facilities;

namespace Polymerium.Avalonia.Models;

public sealed class SettingsSectionModel : ModelBase
{
    #region Direct

    public Symbol Icon { get; set; }

    public required SettingsEntry Target { get; set; }

    #endregion
}
