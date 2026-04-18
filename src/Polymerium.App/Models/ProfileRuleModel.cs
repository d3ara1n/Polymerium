using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Facilities;
using Trident.Abstractions.FileModels;

namespace Polymerium.App.Models;

public partial class ProfileRuleModel(Profile.Rice.Rule owner) : ModelBase
{
    #region Direct

    public Profile.Rice.Rule Owner => owner;

    public ProfileRuleSelectorModel Selector { get; } = new(owner.Selector);

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial bool IsEnabled { get; set; } = owner.Enabled;

    partial void OnIsEnabledChanged(bool value) => owner.Enabled = value;

    [ObservableProperty]
    public partial string Destination { get; set; } = owner.Destination ?? string.Empty;

    partial void OnDestinationChanged(string value) =>
        owner.Destination = !string.IsNullOrWhiteSpace(value) ? value : null;

    [ObservableProperty]
    public partial bool Skipping { get; set; } = owner.Skipping;

    partial void OnSkippingChanged(bool value) => owner.Skipping = value;

    #endregion
}
