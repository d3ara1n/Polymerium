using CommunityToolkit.Mvvm.Input;
using DotNext;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.App.Tasks;
using Polymerium.Trident.Extractors;
using Polymerium.Trident.Helpers;
using Polymerium.Trident.Services;
using Polymerium.Trident.Services.Extracting;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Input;
using Trident.Abstractions.Errors;
using Trident.Abstractions.Resources;
using Windows.ApplicationModel.DataTransfer;

namespace Polymerium.App.ViewModels
{
    public class ModpackViewModel : ViewModelBase
    {
        private readonly DispatcherQueue _dispatcher;
        private readonly ModpackExtractor _extractor;
        private readonly IHttpClientFactory _factory;
        private readonly ILogger _logger;
        private readonly NotificationService _notificationService;
        private readonly RepositoryAgent _repositoryAgent;
        private readonly TaskService _taskService;
        private ExhibitModel? _modpackModel;
        private DataLoadingState dataState = DataLoadingState.Loading;
        private string failureReason = string.Empty;

        private ModpackModel project = ModpackModel.DUMMY;
        private ModpackVersionModel? selectedVersion;

        public ModpackViewModel(RepositoryAgent repositoryAgent, TaskService taskService,
            NotificationService notificationService, IHttpClientFactory factory, ModpackExtractor extractor,
            ILogger<ModpackViewModel> logger)
        {
            _dispatcher = DispatcherQueue.GetForCurrentThread();
            _repositoryAgent = repositoryAgent;
            _taskService = taskService;
            _factory = factory;
            _extractor = extractor;
            _logger = logger;
            _notificationService = notificationService;
            GoBackCommand = new RelayCommand(GoBack);
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

        public ICommand GoBackCommand { get; }
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
            {
                try
                {
                    got = await _repositoryAgent.QueryAsync(_modpackModel.Inner.Label, _modpackModel.Inner.Id);
                }
                catch (Exception e)
                {
                    exception = e;
                }
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
            {
                Process.Start(new ProcessStartInfo(reference.AbsoluteUri) { UseShellExecute = true });
            }
        }

        private void InstallModpack(ModpackVersionModel? version)
        {
            if (version != null)
            {
                InstallModpackTask task = new($"{Project.Inner.Label}:{Project.Inner.Id}/{version.Inner.Id}");
                Task.Run(async () => await InstallModpackAsync(task, Project.Inner, version.Inner));
                _taskService.Enqueue(task);
            }
        }

        private async Task InstallModpackAsync(InstallModpackTask task, Project project, Project.Version version)
        {
            _logger.LogInformation("Start install modpack {project}({version}) from {repo}", project.Id, version.Id,
                project.Label);
            task.OnDownload();
            using HttpClient client = _factory.CreateClient();
            Stream stream = await client.GetStreamAsync(version.Download);
            await using MemoryStream memory = new();
            await stream.CopyToAsync(memory);
            memory.Position = 0;
            task.OnExtract();
            Result<FlattenExtractedContainer, ExtractError> result =
                await _extractor.ExtractAsync(memory, (project, version), App.Current.Token);
            if (result.IsSuccessful)
            {
                FlattenExtractedContainer container = result.Value;
                _logger.LogInformation("Downloaded extracted modpack {name} ready to solidify",
                    container.Original.Name);
                task.OnExport();
                await _extractor.SolidifyAsync(container, null);
                _logger.LogInformation("Solidified {name} as an managed instance", container.Original.Name);
                task.OnFinish();
            }
            else
            {
                _logger.LogError("Install modpack failed for {error}", result.Error);
                ExtractException e = new(result.Error,
                    PurlHelper.MakePurl(project.Label, project.Id, version.Id));
                task.OnError(e);
                throw e;
            }
        }

        private void GoBack()
        {
            SelectedVersion = null;
        }

        private void CopyToClipboard(string? content)
        {
            if (!string.IsNullOrEmpty(content))
            {
                DataPackage package = new() { RequestedOperation = DataPackageOperation.Copy };
                package.SetText(content);
                Clipboard.SetContent(package);
            }
        }
    }
}