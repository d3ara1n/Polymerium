using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Collections;
using Microsoft.UI.Xaml.Media.Animation;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Polymerium.Trident.Repositories;
using Polymerium.Trident.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Resources;

namespace Polymerium.App.ViewModels;

public class MarketViewModel : ObservableObject
{
    private readonly NavigationService _navigation;
    private readonly RepositoryAgent _repositoryAgent;

    private readonly Filter filter = new(null, null, ResourceKind.Modpack);

    private IncrementalLoadingCollection<IncrementalFactorySource<ExhibitModel>, ExhibitModel>? results;

    public MarketViewModel(RepositoryAgent repositoryAgent, NavigationService navigation)
    {
        _repositoryAgent = repositoryAgent;
        _navigation = navigation;
        Repositories = repositoryAgent.Repositories.Select(x => new RepositoryModel(x.Label, x.Label switch
        {
            RepositoryLabels.CURSEFORGE => AssetPath.HEADER_CURSEFORGE,
            RepositoryLabels.MODRINTH => AssetPath.HEADER_MODRINTH,
            _ => throw new NotImplementedException()
        }));
        GotoModpackViewCommand = new RelayCommand<ExhibitModel>(GotoModpackView);
    }

    public ICommand GotoModpackViewCommand { get; }
    public IEnumerable<RepositoryModel> Repositories { get; }

    public IncrementalLoadingCollection<IncrementalFactorySource<ExhibitModel>, ExhibitModel>? Results
    {
        get => results;
        set => SetProperty(ref results, value);
    }

    private void GotoModpackView(ExhibitModel? model)
    {
        if (model != null)
        {
            _navigation.Navigate(typeof(ModpackView), model,
                new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });
        }
    }

    public void UpdateSource(string label, string query) =>
        // TODO: display start loading status and error in ui
        Results = new IncrementalLoadingCollection<IncrementalFactorySource<ExhibitModel>, ExhibitModel>(
            new IncrementalFactorySource<ExhibitModel>(async (page, limit, token) =>
                (await _repositoryAgent.SearchAsync(label, query, page, limit, filter, token)).Select(x =>
                    new ExhibitModel(x, GotoModpackViewCommand))),
            10);
}