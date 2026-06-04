using CommunityToolkit.Mvvm.ComponentModel;
using Polymerium.App.Facilities;
using TridentCore.Abstractions.FileModels;
using TridentCore.Abstractions.Repositories.Resources;

namespace Polymerium.App.Models;

public partial class ProfileRuleSelectorModel(Profile.Rice.Rule.RuleSelector owner) : ModelBase
{
    #region Direct

    public Profile.Rice.Rule.RuleSelector Owner => owner;

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial Profile.Rice.Rule.RuleSelector.SelectorType Type { get; set; } = owner.Type;

    partial void OnTypeChanged(Profile.Rice.Rule.RuleSelector.SelectorType value)
    {
        owner.Type = value;

        if (
            value
            is Profile.Rice.Rule.RuleSelector.SelectorType.AND
                or Profile.Rice.Rule.RuleSelector.SelectorType.OR
                or Profile.Rice.Rule.RuleSelector.SelectorType.NOT
        )
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
    public partial MappingCollection<
        Profile.Rice.Rule.RuleSelector,
        ProfileRuleSelectorModel
    >? Children
    { get; private set; } =
        owner.Children is not null ? new(owner.Children, x => new(x), x => x.Owner) : null;

    [ObservableProperty]
    public partial string Purl { get; set; } = owner.Purl ?? string.Empty;

    partial void OnPurlChanged(string value) =>
        owner.Purl = !string.IsNullOrWhiteSpace(value) ? value : null;

    [ObservableProperty]
    public partial string Repository { get; set; } = owner.Repository ?? string.Empty;

    partial void OnRepositoryChanged(string value) =>
        owner.Repository = !string.IsNullOrWhiteSpace(value) ? value : null;

    [ObservableProperty]
    public partial string Tag { get; set; } = owner.Tag ?? string.Empty;

    partial void OnTagChanged(string value) =>
        owner.Tag = !string.IsNullOrWhiteSpace(value) ? value : null;

    [ObservableProperty]
    public partial ResourceKind Kind { get; set; } = owner.Kind ?? ResourceKind.UNKNOWN;

    partial void OnKindChanged(ResourceKind value) =>
        owner.Kind = value != ResourceKind.UNKNOWN ? value : null;

    #endregion
}
