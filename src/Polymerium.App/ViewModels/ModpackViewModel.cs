using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.Trident.Helpers;
using Polymerium.Trident.Services;
using Polymerium.Trident.Tasks;
using Trident.Abstractions.Resources;

namespace Polymerium.App.ViewModels;

public class ModpackViewModel : ViewModelBase
{
    private readonly DispatcherQueue _dispatcher;
    private readonly NotificationService _notificationService;
    private readonly RepositoryAgent _repositoryAgent;
    private readonly TaskService _taskService;
    private ExhibitModel? _modpackModel;
    private DataLoadingState dataState = DataLoadingState.Loading;

    private ProjectModel project = ProjectModel.DUMMY;
    private ProjectVersionModel? selectedVersion;

    public ModpackViewModel(RepositoryAgent repositoryAgent, TaskService taskService,
        NotificationService notificationService)
    {
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        _repositoryAgent = repositoryAgent;
        _taskService = taskService;
        _notificationService = notificationService;
        OpenReferenceCommand = new RelayCommand<Uri>(OpenReference);
        InstallModpackCommand = new RelayCommand<ProjectVersionModel>(InstallModpack);
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
    public ICommand InstallModpackCommand { get; }

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
            var result = await _repositoryAgent.QueryAsync(_modpackModel.Inner.Label, _modpackModel.Inner.Id);
            if (result.IsSuccessful) got = result.Value;
        }

        _dispatcher.TryEnqueue(() =>
        {
            if (got != null)
            {
                Project = new ProjectModel(got);
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

    private void InstallModpack(ProjectVersionModel? version)
    {
        if (version != null)
        {
            var task = _taskService.Create<InstallModpackTask>(Project.Inner, version.Inner, PurlHelper.MakePurl(
                Project.Inner.Label,
                Project.Inner.Id,
                version.Inner.Id));
            _taskService.Enqueue(task);
        }
    }
}