using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Windows.Foundation;
using Windows.UI;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Polymerium.Trident.Repositories;
using Polymerium.Trident.Services;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Resources;

namespace Polymerium.App.ViewModels;

public class MarketViewModel : ViewModelBase
{
    private readonly NavigationService _navigation;
    private readonly RepositoryAgent repositoryAgent;

    private readonly Filter FILTER = new(null, null, ResourceKind.Modpack);

    private IncrementalLoadingCollection<IncrementalFactorySource<ExhibitModel>, ExhibitModel>? results;

    public MarketViewModel(RepositoryAgent repositoryAgent, NavigationService navigation)
    {
        this.repositoryAgent = repositoryAgent;
        _navigation = navigation;
        Repositories = repositoryAgent.Repositories.Select(x =>
        {
            ((byte, byte, byte), (byte, byte, byte)) color = x.Label switch
            {
                RepositoryLabels.CURSEFORGE => ((246, 211, 101), (253, 160, 133)),
                RepositoryLabels.MODRINTH => ((212, 252, 121), (150, 230, 161)),
                _ => throw new NotImplementedException()
            };
            return new RepositoryModel(x.Label,
                new LinearGradientBrush
                {
                    StartPoint = new Point(1, 0),
                    EndPoint = new Point(0, 1),
                    GradientStops =
                    [
                        new GradientStop
                        {
                            Offset = 0,
                            Color = Color.FromArgb(255, color.Item1.Item1, color.Item1.Item2, color.Item1.Item3)
                        },

                        new GradientStop
                        {
                            Offset = 1,
                            Color = Color.FromArgb(255, color.Item2.Item1, color.Item2.Item2, color.Item2.Item3)
                        }
                    ]
                });
        });
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
            _navigation.Navigate(typeof(ModpackView), model, new SlideNavigationTransitionInfo
            {
                Effect = SlideNavigationTransitionEffect.FromRight
            });
    }

    public void UpdateSource(string label, string query)
    {
        Results = new IncrementalLoadingCollection<IncrementalFactorySource<ExhibitModel>, ExhibitModel>(
            new IncrementalFactorySource<ExhibitModel>(async (page, limit, token) =>
                (await repositoryAgent.SearchAsync(label, query, page, limit, FILTER, token)).Select(x =>
                    new ExhibitModel(x, label, GotoModpackViewCommand))),
            10);
    }
}