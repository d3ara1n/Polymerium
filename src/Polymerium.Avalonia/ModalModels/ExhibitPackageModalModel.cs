using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Models;
using Huskui.Avalonia.Mvvm.Activation;
using Huskui.Avalonia.Mvvm.States;
using Polymerium.Avalonia.Assets;
using Polymerium.Avalonia.Facilities;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Services;
using Polymerium.Avalonia.Utilities;
using TridentCore.Abstractions.Repositories;
using TridentCore.Abstractions.Repositories.Resources;
using TridentCore.Abstractions.Utilities;

namespace Polymerium.Avalonia.ModalModels;

public partial class ExhibitPackageModalModel(
    IViewContext<ExhibitPackageModalModel.Parameter> context,
    DataService dataService,
    PersistenceService persistenceService) : ViewModelBase, IStatefulViewModel<ExhibitPackageModalModel.State>
{
    private readonly Parameter _parameter = context.GetRequiredParameter();

    #region Nested type: Parameter

    public sealed record Parameter(
        string Key,
        ExhibitModel Exhibit,
        ExhibitPackageModel Package,
        Filter Filter,
        Action<ExhibitModel> ModifyPendingCallback,
        Action<ExhibitModel> UndoCallback,
        Func<Project, InstanceExhibitModel> LinkExhibitCallback,
        ICommand ViewPackageCommand);

    #endregion

    #region Nested type: State

    public partial class State : ModelBase
    {
        [ObservableProperty]
        public partial bool IsDetailPanelVisible { get; set; } = true;
    }

    #endregion

    #region Direct

    public ExhibitModel Exhibit => _parameter.Exhibit;
    public ExhibitPackageModel Package => _parameter.Package;
    public ICommand ViewPackageCommand => _parameter.ViewPackageCommand;

    internal Action? DismissHandler { get; set; }

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial bool IsFilterEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool IsFavorite { get; set; }

    [ObservableProperty]
    public partial ExhibitVersionModel? SelectedVersion { get; set; }

    [ObservableProperty]
    public partial int SelectedVersionMode { get; set; }

    [ObservableProperty]
    public partial LazyObject? LazyVersions { get; set; }

    [ObservableProperty]
    public partial LazyObject? LazyDescription { get; set; }

    [ObservableProperty]
    public partial LazyObject? LazyDependencies { get; set; }

    [ObservableProperty]
    public partial LazyObject? LazyChangelog { get; set; }

    [ObservableProperty]
    public partial LazyObject? LazyHistory { get; set; }

    [ObservableProperty]
    public partial State? ViewState { get; set; }

    partial void OnIsFilterEnabledChanged(bool value)
    {
        LazyVersions = ConstructVersions();
        LazyDependencies = ConstructDependencies();
    }

    partial void OnSelectedVersionChanged(ExhibitVersionModel? value)
    {
        LazyDependencies = ConstructDependencies();
        LazyChangelog = ConstructChangelog();
        ApplyCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedVersionModeChanged(int value)
    {
        LazyDependencies = ConstructDependencies();
        ApplyCommand.NotifyCanExecuteChanged();
    }

    #endregion

    #region Overrides

    protected override Task OnInitializeAsync(CancellationToken token)
    {
        var package = _parameter.Package;
        IsFavorite = persistenceService.IsFavoriteProject(package.Label, package.Namespace, package.ProjectId);
        LazyVersions = ConstructVersions();
        LazyDescription = ConstructDescription();
        LazyChangelog = ConstructChangelog();
        LazyDependencies = ConstructDependencies();
        LazyHistory = ConstructHistory();
        return Task.CompletedTask;
    }

    #endregion

    #region Lazy construction

    private LazyObject ConstructDescription() =>
        new(async t =>
        {
            if (t.IsCancellationRequested)
            {
                return null;
            }

            var package = _parameter.Package;
            return await dataService.ReadDescriptionAsync(new(package.Label, package.Namespace, package.ProjectId));
        });

    private LazyObject ConstructChangelog() =>
        new(async t =>
        {
            if (t.IsCancellationRequested)
            {
                return null;
            }

            var vid = SelectedVersionMode == 0 ? SelectedVersion?.VersionId : null;
            if (vid is null)
            {
                return null;
            }

            var package = _parameter.Package;
            return await dataService.ReadChangelogAsync(new(package.Label, package.Namespace, package.ProjectId, vid));
        });

    private LazyObject ConstructDependencies() =>
        new(async t =>
        {
            if (t.IsCancellationRequested)
            {
                return null;
            }

            var vid = SelectedVersionMode == 0 ? SelectedVersion?.VersionId : null;
            var package = _parameter.Package;
            var resolved = await dataService.ResolvePackageAsync(
                new(package.Label, package.Namespace, package.ProjectId, vid),
                _parameter.Filter);
            var tasks = resolved
                       .Dependencies.Select(async x =>
                        {
                            var dependency = await dataService.QueryProjectAsync(new(x.Label,
                                                 x.Namespace,
                                                 x.ProjectId));
                            return new ExhibitDependencyModel(_parameter.LinkExhibitCallback(dependency),
                                                              x.Label,
                                                              x.Namespace,
                                                              x.ProjectId,
                                                              x.VersionId,
                                                              dependency.ProjectName,
                                                              dependency.Thumbnail ?? AssetUriIndex.DirtImage,
                                                              dependency.Author,
                                                              dependency.Kind,
                                                              x.IsRequired);
                        })
                       .ToArray();
            await Task.WhenAll(tasks);
            return new ExhibitDependencyCollection(resolved.VersionName, resolved.VersionId, [.. tasks.Select(x => x.Result)]);
        });

    private LazyObject ConstructVersions() =>
        new(async t =>
            {
                if (t.IsCancellationRequested)
                {
                    return null;
                }

                var package = _parameter.Package;
                var versions = (await dataService.InspectVersionsAsync(package.Label,
                                                    package.Namespace,
                                                    package.ProjectId,
                                                    IsFilterEnabled ? _parameter.Filter : Filter.None)).ToArray();
                var rv = new ExhibitVersionCollection([
                    .. versions.Select(x => new ExhibitVersionModel(package.Label,
                                             package.Namespace,
                                             package.ProjectName,
                                             package.ProjectId,
                                             x.VersionName,
                                             x.VersionId,
                                             string.Join(",",
                                                         x.Requirements.AnyOfLoaders
                                                          .Select(LoaderHelper.ToDisplayName)),
                                             string.Join(",", x.Requirements.AnyOfVersions),
                                             string.Empty,
                                             x.PublishedAt,
                                             x.DownloadCount,
                                             x.ReleaseType,
                                             PackageHelper.ToPref(package.Label,
                                                 package.Namespace,
                                                 package.ProjectId,
                                                 x.VersionId)))
                ]);
                return rv;
            },
            value =>
            {
                var exhibit = _parameter.Exhibit;
                var versionId = exhibit.State switch
                {
                    null or ExhibitState.Editable or ExhibitState.Removing => exhibit.InstalledVersionId,
                    _ => exhibit.PendingVersionId
                };

                if (value is ExhibitVersionCollection versions)
                {
                    if (versionId != null)
                    {
                        var installed = versions.FirstOrDefault(x => x.VersionId == versionId);
                        if (installed != null)
                        {
                            Dispatcher.UIThread.Post(() =>
                            {
                                SelectedVersion = installed;
                                SelectedVersionMode = 0;
                            });
                        }
                    }
                    else
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            SelectedVersion = versions.FirstOrDefault();
                            SelectedVersionMode = 0;
                        });
                    }
                }
            });

    private LazyObject ConstructHistory() =>
        new(async t =>
        {
            if (t.IsCancellationRequested)
            {
                return null;
            }

            var package = _parameter.Package;
            var actions = persistenceService.GetActions(_parameter.Key);

            var filteredActions = actions
                                 .Where(x => (x.Old is not null
                                           && PackageHelper.IsMatched(x.Old,
                                                                      package.Label,
                                                                      package.Namespace,
                                                                      package.ProjectId))
                                          || (x.New is not null
                                           && PackageHelper.IsMatched(x.New,
                                                                      package.Label,
                                                                      package.Namespace,
                                                                      package.ProjectId)))
                                 .ToArray();

            var tasks = filteredActions
                       .Select(async x =>
                        {
                            if (x.New != null && PackageHelper.TryParse(x.New, out var result))
                            {
                                if (result.Version is null)
                                {
                                    if (x.Old is null)
                                    {
                                        // NOTE: null -> Project（AddUnversioned）
                                        return new()
                                        {
                                            Kind = InstancePackageModificationKind.AddUnversioned,
                                            VersionName = null,
                                            ModifiedAtRaw = DateTimeHelper.FromPersistedLocalDateTime(x.At)
                                        };
                                    }

                                    // NOTE: -> Project: Unset
                                    return new()
                                    {
                                        Kind = InstancePackageModificationKind.Unset,
                                        VersionName = null,
                                        ModifiedAtRaw = DateTimeHelper.FromPersistedLocalDateTime(x.At)
                                    };
                                }

                                var resolved = await dataService.ResolvePackageAsync(result, _parameter.Filter);
                                if (x.Old is null)
                                {
                                    // NOTE: null -> Package: Add
                                    return new()
                                    {
                                        Kind = InstancePackageModificationKind.AddVersioned,
                                        VersionName = resolved.VersionName,
                                        ModifiedAtRaw = DateTimeHelper.FromPersistedLocalDateTime(x.At)
                                    };
                                }

                                // NOTE: Package -> Package: Update
                                return new()
                                {
                                    Kind = InstancePackageModificationKind.Update,
                                    VersionName = resolved.VersionName,
                                    ModifiedAtRaw = DateTimeHelper.FromPersistedLocalDateTime(x.At)
                                };
                            }

                            return new InstancePackageModificationModel
                            {
                                Kind = InstancePackageModificationKind.Remove,
                                VersionName = null,
                                ModifiedAtRaw = DateTimeHelper.FromPersistedLocalDateTime(x.At)
                            };
                        })
                       .ToArray();

            await Task.WhenAll(tasks);
            var results = tasks.Where(x => x.IsCompletedSuccessfully).Select(x => x.Result).ToList();
            return new InstancePackageModificationCollection(results);
        });

    #endregion

    #region Commands

    private bool CanApply() => SelectedVersionMode == 1 || (SelectedVersionMode == 0 && SelectedVersion != null);

    [RelayCommand(CanExecute = nameof(CanApply))]
    private void Apply()
    {
        var exhibit = _parameter.Exhibit;
        if (exhibit.State is null)
        {
            exhibit.State = ExhibitState.Adding;
        }

        if (exhibit.State is ExhibitState.Editable)
        {
            if (exhibit.InstalledVersionId == SelectedVersion?.VersionId)
            {
                DismissHandler?.Invoke();
                return;
            }

            exhibit.State = ExhibitState.Modifying;
        }

        if (SelectedVersionMode == 0 && SelectedVersion != null)
        {
            exhibit.PendingVersionId = SelectedVersion?.VersionId;
            exhibit.PendingVersionName = SelectedVersion?.VersionName;
        }
        else
        {
            exhibit.PendingVersionId = null;
            exhibit.PendingVersionName = null;
        }

        _parameter.ModifyPendingCallback(exhibit);
        DismissHandler?.Invoke();
    }

    [RelayCommand]
    private void Delete()
    {
        var exhibit = _parameter.Exhibit;
        exhibit.State = ExhibitState.Removing;
        exhibit.PendingVersionId = null;
        exhibit.PendingVersionName = null;
        _parameter.ModifyPendingCallback(exhibit);
        DismissHandler?.Invoke();
    }

    [RelayCommand]
    private void Undo()
    {
        var exhibit = _parameter.Exhibit;
        exhibit.PendingVersionId = null;
        exhibit.PendingVersionName = null;
        _parameter.UndoCallback(exhibit);
        DismissHandler?.Invoke();
    }

    [RelayCommand]
    private void Favorite()
    {
        var package = _parameter.Package;
        if (IsFavorite)
        {
            persistenceService.RemoveFavoriteProject(package.Label, package.Namespace, package.ProjectId);
            IsFavorite = false;
            return;
        }

        persistenceService.AddFavoriteProject(package.Label,
                                              package.Namespace,
                                              package.ProjectId,
                                              package.ProjectName,
                                              package.AuthorName,
                                              package.Summary,
                                              package.Reference ?? Exhibit.Reference,
                                              package.Thumbnail,
                                              _parameter.Filter.Kind ?? ResourceKind.Unknown,
                                              package.DownloadCountRaw,
                                              package.Tags,
                                              package.UpdatedAtRaw,
                                              package.UpdatedAtRaw);
        IsFavorite = true;
    }

    #endregion
}
