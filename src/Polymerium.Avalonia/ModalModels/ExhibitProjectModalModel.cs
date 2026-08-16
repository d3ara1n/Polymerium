using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Models;
using Huskui.Avalonia.Mvvm.Activation;
using Huskui.Avalonia.Mvvm.States;
using Polymerium.Avalonia.Facilities;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Services;
using TridentCore.Abstractions.Repositories.Resources;

namespace Polymerium.Avalonia.ModalModels;

public partial class ExhibitProjectModalModel(
    IViewContext<ExhibitProjectModalModel.Parameter> context,
    DataService dataService,
    PersistenceService persistenceService) : ViewModelBase, IStatefulViewModel<ExhibitProjectModalModel.State>
{
    private readonly Parameter _parameter = context.GetRequiredParameter();

    #region Nested types

    public sealed record Parameter(
        ExhibitModel Exhibit,
        ExhibitPackageModel Package,
        ResourceKind Kind,
        Action<ExhibitModel> ModifyPendingCallback,
        Action<ExhibitModel> UndoCallback);

    public partial class State : ModelBase
    {
        [ObservableProperty]
        public partial bool IsDetailPanelVisible { get; set; } = true;
    }

    #endregion

    #region Direct

    public ExhibitModel Exhibit => _parameter.Exhibit;
    public ExhibitPackageModel Package => _parameter.Package;

    internal Action? DismissHandler { get; set; }

    #endregion

    #region Reactive

    [ObservableProperty]
    public partial bool IsFavorite { get; set; }

    [ObservableProperty]
    public partial LazyObject? LazyDescription { get; set; }

    [ObservableProperty]
    public partial State? ViewState { get; set; }

    #endregion

    #region Overrides

    protected override Task OnInitializeAsync(CancellationToken token)
    {
        var package = _parameter.Package;
        IsFavorite = persistenceService.IsFavoriteProject(package.Label, package.Namespace, package.ProjectId);
        LazyDescription = ConstructDescription();
        return Task.CompletedTask;
    }

    #endregion

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

    #region Commands

    [RelayCommand]
    private void Add()
    {
        var exhibit = _parameter.Exhibit;
        exhibit.State = ExhibitState.Adding;
        _parameter.ModifyPendingCallback(exhibit);
        DismissHandler?.Invoke();
    }

    [RelayCommand]
    private void Remove()
    {
        var exhibit = _parameter.Exhibit;
        exhibit.State = ExhibitState.Removing;
        _parameter.ModifyPendingCallback(exhibit);
        DismissHandler?.Invoke();
    }

    [RelayCommand]
    private void Undo()
    {
        _parameter.UndoCallback(_parameter.Exhibit);
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
                                              _parameter.Kind,
                                              package.DownloadCountRaw,
                                              package.Tags,
                                              package.UpdatedAtRaw,
                                              package.UpdatedAtRaw);
        IsFavorite = true;
    }

    #endregion
}
