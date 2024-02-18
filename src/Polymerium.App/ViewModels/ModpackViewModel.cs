using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.Trident.Services;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using Trident.Abstractions.Resources;
using Windows.ApplicationModel.DataTransfer;

namespace Polymerium.App.ViewModels;

public class ModpackViewModel : ViewModelBase
{
    private readonly DispatcherQueue _dispatcher;
    private readonly NotificationService _notificationService;
    private readonly RepositoryAgent _repositoryAgent;
    private readonly TaskService _taskService;
    private ExhibitModel? _modpackModel;
    private DataLoadingState dataState = DataLoadingState.Loading;
    private string failureReason = string.Empty;

    private ModpackModel project = ModpackModel.DUMMY;
    private ModpackVersionModel? selectedVersion;

    public ModpackViewModel(RepositoryAgent repositoryAgent, TaskService taskService,
        NotificationService notificationService)
    {
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        _repositoryAgent = repositoryAgent;
        _taskService = taskService;
        _notificationService = notificationService;
        OpenReferenceCommand = new RelayCommand<Uri>(OpenReference);
        InstallModpackCommand = new RelayCommand<ModpackVersionModel>(InstallModpack);
        CopyToClipboardCommand = new RelayCommand<string>(CopyToClipboard);
    }

    public DataLoadingState DataState
    {
        get => dataState;
        set => SetProperty(ref dataState, value);
    }

    public ModpackModel Project
    {
        get => project;
        set => SetProperty(ref project, value);
    }

    public ModpackVersionModel? SelectedVersion
    {
        get => selectedVersion;
        set => SetProperty(ref selectedVersion, value);
    }

    public string FailureReason
    {
        get => failureReason;
        set => SetProperty(ref failureReason, value);
    }

    public ICommand OpenReferenceCommand { get; }
    public ICommand InstallModpackCommand { get; }
    public ICommand CopyToClipboardCommand { get; }

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
        Exception? exception = null;
        if (_modpackModel != null)
            try
            {
                got = await _repositoryAgent.QueryAsync(_modpackModel.Inner.Label, _modpackModel.Inner.Id);
            }
            catch (Exception e)
            {
                exception = e;
            }

        _dispatcher.TryEnqueue(() =>
        {
            if (got != null)
            {
                Project = new ModpackModel(got);
                DataState = DataLoadingState.Done;
            }
            else
            {
                FailureReason = exception?.Message ?? "Unknown";
                DataState = DataLoadingState.Failed;
            }
        });
    }

    private void OpenReference(Uri? reference)
    {
        if (reference != null)
            Process.Start(new ProcessStartInfo(reference.AbsoluteUri) { UseShellExecute = true });
    }

    private void InstallModpack(ModpackVersionModel? version)
    {
        //if (version != null)
        //{
        //    var task = _taskService.Create<InstallModpackTask>(Project.Inner, version.Inner, PurlHelper.MakePurl(
        //        Project.Inner.Label,
        //        Project.Inner.Id,
        //        version.Inner.Id));
        //    _taskService.Enqueue(task);
        //}

        //protected override async Task OnThreadAsync()
        //{
        //    ReportProgress(status: "Downloading pack file...");
        //    using var client = factory.CreateClient();
        //    var stream = await client.GetStreamAsync(version.Download);
        //    await using var memory = new MemoryStream();
        //    await stream.CopyToAsync(memory);
        //    memory.Position = 0;
        //    ReportProgress(status: "Extracting metadata...");
        //    var result = await extractor.ExtractAsync(memory, (project, version), Token);
        //    if (result.IsSuccessful)
        //    {
        //        var container = result.Value;
        //        logger.LogInformation("Downloaded extracted modpack {name} ready to solidify", container.Original.Name);
        //        ReportProgress(status: "Exporting data & files...");
        //        await extractor.SolidifyAsync(container, null);
        //        logger.LogInformation("Solidified {name} as an managed instance", container.Original.Name);
        //    }
        //    else
        //    {
        //        logger.LogError("Install modpack failed for {error}", result.Error);
        //        throw new ExtractException(result.Error, PurlHelper.MakePurl(project.Label, project.Id, version.Id));
        //    }
        //}
    }

    private void CopyToClipboard(string? content)
    {
        if (!string.IsNullOrEmpty(content))
        {
            var package = new DataPackage { RequestedOperation = DataPackageOperation.Copy };
            package.SetText(content);
            Clipboard.SetContent(package);
        }
    }
}