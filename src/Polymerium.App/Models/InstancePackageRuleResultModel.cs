using Polymerium.App.Facilities;
using Trident.Abstractions.Utilities;

namespace Polymerium.App.Models;

public class InstancePackageRuleResultModel(RuleHelper.Result result) : ModelBase
{
    public int AppliedRuleCount => result.AppliedRules.Count;
    public bool Matched => result.Matched;
    public string? Destination => result.EffectiveRule?.Destination;
    public bool Solidifying => result.EffectiveRule?.Solidifying ?? false;
    public bool Skipping => result.EffectiveRule?.Skipping ?? false;
}
