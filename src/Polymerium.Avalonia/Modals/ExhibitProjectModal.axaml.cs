using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Models;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Services;
using Polymerium.Avalonia.Utilities;
using TridentCore.Abstractions.Repositories.Resources;
using AppResources = Polymerium.Avalonia.Properties.Resources;

namespace Polymerium.Avalonia.Modals;

public partial class ExhibitProjectModal : Modal
{
    public static readonly DirectProperty<ExhibitProjectModal, ExhibitModel> ExhibitProperty =
        AvaloniaProperty.RegisterDirect<ExhibitProjectModal, ExhibitModel>(nameof(Exhibit),
                                                                           o => o.Exhibit,
                                                                           (o, v) => o.Exhibit = v);

    public static readonly DirectProperty<ExhibitProjectModal, bool> IsFavoriteProperty =
        AvaloniaProperty.RegisterDirect<ExhibitProjectModal, bool>(nameof(IsFavorite),
                                                                   o => o.IsFavorite,
                                                                   (o, v) => o.IsFavorite = v);

    public static readonly DirectProperty<ExhibitProjectModal, LazyObject?> LazyDescriptionProperty =
        AvaloniaProperty.RegisterDirect<ExhibitProjectModal, LazyObject?>(nameof(LazyDescription),
                                                                          o => o.LazyDescription,
                                                                          (o, v) => o.LazyDescription = v);

    public static readonly DirectProperty<ExhibitProjectModal, bool> IsDetailPanelVisibleProperty =
        AvaloniaProperty.RegisterDirect<ExhibitProjectModal, bool>(nameof(IsDetailPanelVisible),
                                                                   o => o.IsDetailPanelVisible,
                                                                   (o, v) => o.IsDetailPanelVisible = v);

    private static bool isDetailPanelVisible;

    public ExhibitProjectModal() => InitializeComponent();

    public required DataService DataService { get; init; }

    public required PersistenceService PersistenceService { get; init; }

    public required ResourceKind Kind { get; init; }

    public required Action<ExhibitModel> ModifyPendingCallback { get; init; }
    public required Action<ExhibitModel> UndoCallback { get; init; }

    public required ExhibitModel Exhibit
    {
        get;
        set => SetAndRaise(ExhibitProperty, ref field, value);
    }

    public bool IsFavorite
    {
        get;
        set => SetAndRaise(IsFavoriteProperty, ref field, value);
    }

    public LazyObject? LazyDescription
    {
        get;
        set => SetAndRaise(LazyDescriptionProperty, ref field, value);
    }

    public bool IsDetailPanelVisible
    {
        get;
        set => SetAndRaise(IsDetailPanelVisibleProperty, ref field, value);
    } = isDetailPanelVisible;

    private ExhibitPackageModel Package => (DataContext as ExhibitPackageModel)!;

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        IsFavorite = PersistenceService.IsFavoriteProject(Package.Label, Package.Namespace, Package.ProjectId);
        LazyDescription = ConstructDescription();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsDetailPanelVisibleProperty)
        {
            isDetailPanelVisible = change.GetNewValue<bool>();
        }
    }

    private LazyObject ConstructDescription()
    {
        var lazy = new LazyObject(async t =>
        {
            if (t.IsCancellationRequested)
            {
                return null;
            }

            var description =
                await DataService.ReadDescriptionAsync(new(Package.Label, Package.Namespace, Package.ProjectId));
            return description;
        });
        return lazy;
    }

    #region Commands

    [RelayCommand]
    private void Add()
    {
        Exhibit.State = ExhibitState.Adding;
        ModifyPendingCallback(Exhibit);
        Dismiss();
    }

    [RelayCommand]
    private void Remove()
    {
        Exhibit.State = ExhibitState.Removing;
        ModifyPendingCallback(Exhibit);
        Dismiss();
    }

    [RelayCommand]
    private void Undo()
    {
        UndoCallback(Exhibit);
        Dismiss();
    }

    [RelayCommand]
    private void Favorite()
    {
        if (IsFavorite)
        {
            PersistenceService.RemoveFavoriteProject(Package.Label, Package.Namespace, Package.ProjectId);
            IsFavorite = false;
            return;
        }

        PersistenceService.AddFavoriteProject(Package.Label,
                                              Package.Namespace,
                                              Package.ProjectId,
                                              Package.ProjectName,
                                              Package.AuthorName,
                                              Package.Summary,
                                              Package.Reference ?? Exhibit.Reference,
                                              Package.Thumbnail,
                                              Kind,
                                              Package.DownloadCountRaw,
                                              Package.Tags,
                                              Package.UpdatedAtRaw,
                                              Package.UpdatedAtRaw);
        IsFavorite = true;
    }

    [RelayCommand]
    private Task NavigateUri(string? url)
    {
        if (url is not null && Package.Reference is not null)
        {
            var rev = new Uri(url, UriKind.RelativeOrAbsolute);
            return TopLevelHelper.LaunchUriAsync(TopLevel.GetTopLevel(this),
                                                 rev.IsAbsoluteUri ? rev : new(Package.Reference, rev),
                                                 AppResources
                                                    .ExhibitPackageModal_OpenPackageLinkDangerNotificationTitle);
        }

        return Task.CompletedTask;
    }

    #endregion
}
