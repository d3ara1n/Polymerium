using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using Huskui.Avalonia.Models;
using Huskui.Avalonia.Mvvm.Activation;
using Polymerium.Avalonia.Facilities;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Services;
using Polymerium.Avalonia.Utilities;
using TridentCore.Abstractions.Utilities;
using TridentCore.Core.Services;
using GrowlLevel = Huskui.Avalonia.Models.GrowlLevel;

namespace Polymerium.Avalonia.ModalModels;

public partial class PackageBulkUpdateReviewModalModel(
    IViewContext<PackageBulkUpdateReviewModalModel.Parameter> context,
    DataService dataService,
    PersistenceService persistenceService,
    ProfileManager profileManager,
    NotificationService notificationService) : ViewModelBase
{
    private readonly Parameter _parameter = context.GetRequiredParameter();

    #region Nested type: Parameter

    public sealed record Parameter(string Key, ObservableCollection<PackageBulkUpdateCandidateModel> Candidates);

    #endregion

    #region Direct

    public string Key => _parameter.Key;
    public ObservableCollection<PackageBulkUpdateCandidateModel> Candidates => _parameter.Candidates;

    internal Action? DismissHandler { get; set; }

    private IDisposable? _updateCountSubscription;

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial PackageBulkUpdateCandidateModel? SelectedCandidate { get; set; }

    [ObservableProperty]
    public partial LazyObject? LazyChangelog { get; set; }

    [ObservableProperty]
    public partial int UpdateCount { get; set; }

    partial void OnSelectedCandidateChanged(PackageBulkUpdateCandidateModel? value) => LazyChangelog = ConstructChangelog();

    partial void OnLazyChangelogChanged(LazyObject? oldValue, LazyObject? newValue) => oldValue?.Cancel();

    #endregion

    #region Overrides

    protected override Task OnInitializeAsync(CancellationToken token)
    {
        SelectedCandidate = Candidates.FirstOrDefault();
        _updateCountSubscription = Candidates
                                  .ToObservableChangeSet()
                                  .AutoRefresh(x => x.Decision)
                                  .Filter(x => x.Decision == PackageBulkUpdateDecision.Update)
                                  .QueryWhenChanged(query => query.Count)
                                  .Subscribe(count => UpdateCount = count);
        return Task.CompletedTask;
    }

    protected override Task OnDeinitializeAsync()
    {
        _updateCountSubscription?.Dispose();
        LazyChangelog?.Cancel();
        return Task.CompletedTask;
    }

    #endregion

    private LazyObject ConstructChangelog() =>
        new(async t =>
        {
            if (t.IsCancellationRequested)
            {
                return null;
            }

            var candidate = SelectedCandidate;
            if (candidate is null)
            {
                return null;
            }

            var package = candidate.Package;
            return await dataService.ReadChangelogAsync(new(package.Label,
                                                           package.Namespace,
                                                           package.ProjectId,
                                                           candidate.NewVersionId));
        });

    #region Commands

    [RelayCommand]
    private async Task ApplyAsync()
    {
        foreach (var candidate in Candidates.Where(x =>
                     x.Decision is PackageBulkUpdateDecision.SkipVersion or PackageBulkUpdateDecision.Hold))
        {
            var package = candidate.Package;
            persistenceService.SetUpdateBlacklist(Key,
                                                  package.Label,
                                                  package.Namespace,
                                                  package.ProjectId,
                                                  candidate.Decision == PackageBulkUpdateDecision.SkipVersion
                                                      ? candidate.NewVersionId
                                                      : null);
            candidate.Model.IsUpdateHeld = candidate.Decision == PackageBulkUpdateDecision.Hold;
        }

        var updates = Candidates.Where(x => x.Decision == PackageBulkUpdateDecision.Update).ToList();
        if (updates.Count > 0)
        {
            if (!profileManager.TryGetMutable(Key, out var guard))
            {
                notificationService.PopMessage(LanguageManager.Instance.PackageBulkUpdateReviewModal_ApplyFailedText.Current(),
                                                LanguageManager.Instance.PackageBulkUpdateReviewModal_ApplyFailedTitle.Current(),
                                                GrowlLevel.Danger);
                return;
            }

            await using (guard)
            {
                foreach (var candidate in updates)
                {
                    var old = candidate.Model.Entry.Pref;
                    if (candidate.Model.Info is { } info)
                    {
                        info.Version = ToVersionModel(candidate);
                    }
                    else
                    {
                        candidate.Model.Entry.Pref = PackageHelper.ToPref(candidate.Package.Label,
                                                                          candidate.Package.Namespace,
                                                                          candidate.Package.ProjectId,
                                                                          candidate.NewVersionId);
                    }

                    persistenceService.AppendAction(new()
                    {
                        Key = Key,
                        Kind = PersistenceService.ActionKind.EditPackage,
                        Old = old,
                        New = candidate.Model.Entry.Pref
                    });
                }
            }
        }

        DismissHandler?.Invoke();
    }

    private static InstancePackageVersionModel ToVersionModel(PackageBulkUpdateCandidateModel candidate) =>
        new(candidate.NewVersionId,
            candidate.NewVersionName,
            string.Join(",",
                        candidate.Package.Requirements.AnyOfLoaders.Select(LoaderHelper.ToDisplayName)),
            string.Join(",", candidate.Package.Requirements.AnyOfVersions),
            candidate.NewVersionTime,
            candidate.Package.ReleaseType,
            candidate.Package.Dependencies);

    #endregion
}
