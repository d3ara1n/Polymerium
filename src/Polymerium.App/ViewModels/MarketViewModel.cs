using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Views;
using Polymerium.Trident.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Resources;
using Windows.UI;

namespace Polymerium.App.ViewModels
{
    public class MarketViewModel : ViewModelBase
    {
        public ICommand GotoModpackViewCommand { get; }
        public IEnumerable<RepositoryModel> Repositories { get; }

        private readonly Filter FILTER = new(null, null, ResourceKind.Modpack);

        private readonly NavigationService _navigation;

        public MarketViewModel(IEnumerable<IRepository> repositories, NavigationService navigation)
        {
            _navigation = navigation;
            Repositories = repositories.Select(x =>
            {
                ((byte, byte, byte), (byte, byte, byte)) color = x.Label switch
                {
                    RepositoryLabels.CURSEFORGE => ((246, 211, 101), (253, 160, 133)),
                    RepositoryLabels.MODRINTH => ((212, 252, 121), (150, 230, 161)),
                    _ => throw new NotImplementedException()
                };
                return new RepositoryModel(x,
                    new LinearGradientBrush()
                    {
                        StartPoint = new(1, 0),
                        EndPoint = new(0, 1),
                        GradientStops = new()
                        {
                            new()
                            {
                                Offset = 0,
                                Color = Color.FromArgb(255, color.Item1.Item1, color.Item1.Item2, color.Item1.Item3)
                            },
                            new()
                            {
                                Offset = 1,
                                Color = Color.FromArgb(255, color.Item2.Item1, color.Item2.Item2, color.Item2.Item3)
                            }
                        }
                    });
            });
            GotoModpackViewCommand = new RelayCommand<ExhibitModel>(GotoModpackView);
        }

        private void GotoModpackView(ExhibitModel? model)
        {
            if (model != null) _navigation.Navigate(typeof(ModpackView), model, new SlideNavigationTransitionInfo()
            {
                Effect = SlideNavigationTransitionEffect.FromRight
            });
        }

        public async Task<IEnumerable<ExhibitModel>> SearchAsync(IRepository repository, string query, uint page, uint limit, CancellationToken token)
        {
            return (await repository.SearchAsync(query, page, limit, FILTER, token)).Select(x => new ExhibitModel(x, repository, GotoModpackViewCommand));
        }
    }
}
