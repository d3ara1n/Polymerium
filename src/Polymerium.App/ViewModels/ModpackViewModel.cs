using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Polymerium.App.Models;
using Polymerium.Trident.Services;
using Trident.Abstractions.Resources;

namespace Polymerium.App.ViewModels;

public class ModpackViewModel : ViewModelBase
{
    private readonly DispatcherQueue _dispatcher;
    private readonly RepositoryAgent repositoryAgent;
    private ExhibitModel? _modpackModel;
    private DataLoadingState dataState = DataLoadingState.Loading;

    private ProjectModel project = ProjectModel.DUMMY;
    private ProjectVersionModel? selectedVersion;

    public ModpackViewModel(RepositoryAgent repositoryAgent)
    {
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        this.repositoryAgent = repositoryAgent;
        OpenReferenceCommand = new RelayCommand<Uri>(OpenReference);
    }

    public DataLoadingState DataState
    {
        get => dataState;
        set => SetProperty(ref dataState, value);
    }

    public ProjectModel Project
    {
        get => project;
        set => SetProperty(ref project, value);
    }

    public ProjectVersionModel? SelectedVersion
    {
        get => selectedVersion;
        set => SetProperty(ref selectedVersion, value);
    }

    public ICommand OpenReferenceCommand { get; }

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
        Project? got = null;
        if (_modpackModel != null)
        {
            var result = await repositoryAgent.QueryAsync(_modpackModel.RepositoryLabel, _modpackModel.Inner.Id);
            if (result.IsSuccessful) got = result.Value;
        }

        _dispatcher.TryEnqueue(() =>
        {
            if (got != null)
            {
                Project = new ProjectModel(got, _modpackModel!.RepositoryLabel);
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
        if (reference != null)
            Process.Start(new ProcessStartInfo(reference.AbsoluteUri)
            {
                UseShellExecute = true
            });
    }
}