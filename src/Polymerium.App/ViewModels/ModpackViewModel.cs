using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Polymerium.App.Models;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Trident.Abstractions.Resources;

namespace Polymerium.App.ViewModels
{
    public class ModpackViewModel : ViewModelBase
    {
        private DataLoadingState dataState = DataLoadingState.Loading;
        public DataLoadingState DataState { get => dataState; set => SetProperty(ref dataState, value); }

        private ProjectModel? project;
        public ProjectModel? Project { get => project; set => SetProperty(ref project, value); }

        public ICommand OpenReferenceCommand { get; }

        private DispatcherQueue _dispatcher;
        private ExhibitModel? _modpackModel;

        public ModpackViewModel()
        {
            _dispatcher = DispatcherQueue.GetForCurrentThread();

            OpenReferenceCommand = new RelayCommand<Uri>(OpenReference);
        }

        public override bool OnAttached(object? maybeModpackModel)
        {
            if (maybeModpackModel is ExhibitModel model)
            {
                _modpackModel = model;
                Task.Run(LoadProjectAsync);
            }

            return false;
        }

        private async Task LoadProjectAsync()
        {
            Project? project = null;
            if (_modpackModel != null)
            {
                var result = await _modpackModel.Repository.QueryAsync(_modpackModel.Inner.Id, CancellationToken.None);
                if (result.IsSuccessful)
                {
                    project = result.Value;
                }
            }
            _dispatcher.TryEnqueue(() =>
            {
                if (project != null)
                {
                    Project = new ProjectModel(project, _modpackModel!.Repository);
                    DataState = DataLoadingState.Done;
                }
                else
                {
                    DataState = DataLoadingState.Failed;
                }
            });
        }

        private void OpenReference(Uri? reference)
        {
            if (reference != null) Process.Start(new ProcessStartInfo(reference.AbsoluteUri)
            {
                UseShellExecute = true
            });
        }

    }
}
