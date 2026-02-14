using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Facilities;
using Trident.Abstractions.FileModels;
using Trident.Abstractions.Repositories.Resources;

namespace Polymerium.App.Models;

public partial class ProfileRuleModel(Profile.Rice.Rule owner) : ModelBase
{
    #region Direct

    public Profile.Rice.Rule Owner => owner;

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial bool IsEnabled { get; set; } = owner.Enabled;

    partial void OnIsEnabledChanged(bool value) => owner.Enabled = value;

    [ObservableProperty]
    public partial Profile.Rice.Rule.SelectorType Selector { get; set; } = owner.Selector;

    partial void OnSelectorChanged(Profile.Rice.Rule.SelectorType value)
    {
        owner.Selector = value;

        if (value is Profile.Rice.Rule.SelectorType.And
                  or Profile.Rice.Rule.SelectorType.Or
                  or Profile.Rice.Rule.SelectorType.Not)
        {
            owner.Children ??= [];
            Children ??= new(owner.Children, x => new(x), x => x.Owner);
        }
        else
        {
            if (Children is null || Children.Count == 0)
            {
                owner.Children = null;
                Children = null;
            }
        }
    }

    [ObservableProperty]
    public partial MappingCollection<Profile.Rice.Rule, ProfileRuleModel>? Children { get; set; } =
        owner.Children is not null ? new(owner.Children, x => new(x), x => x.Owner) : null;

    [ObservableProperty]
    public partial string Purl { get; set; } = owner.Purl ?? string.Empty;

    partial void OnPurlChanged(string value) => owner.Purl = !string.IsNullOrWhiteSpace(value) ? value : null;

    [ObservableProperty]
    public partial string Repository { get; set; } = owner.Repository ?? string.Empty;

    partial void OnRepositoryChanged(string value) =>
        owner.Repository = !string.IsNullOrWhiteSpace(value) ? value : null;

    [ObservableProperty]
    public partial string Tag { get; set; } = owner.Tag ?? string.Empty;

    partial void OnTagChanged(string value) => owner.Tag = !string.IsNullOrWhiteSpace(value) ? value : null;

    [ObservableProperty]
    public partial ResourceKind Kind { get; set; } = owner.Kind ?? ResourceKind.Unknown;

    partial void OnKindChanged(ResourceKind value) => owner.Kind = value != ResourceKind.Unknown ? value : null;

    #endregion
}
