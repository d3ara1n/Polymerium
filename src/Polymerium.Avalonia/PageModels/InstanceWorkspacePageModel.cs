using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using Huskui.Avalonia.Models;
using Huskui.Avalonia.Mvvm.Activation;
using LibGit2Sharp;
using Polymerium.Avalonia.Modals;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Services;
using Polymerium.Avalonia.Utilities;
using TridentCore.Abstractions;
using TridentCore.Core.Services;
using TridentCore.Core.Utilities;

namespace Polymerium.Avalonia.PageModels;

public partial class InstanceWorkspacePageModel : InstancePageModelBase
{
    /// <inheritdoc />
    public InstanceWorkspacePageModel(
        IViewContext<InstanceContextParameter> context,
        InstanceStateAggregator aggregator,
        InstanceManager instanceManager,
        NotificationService notificationService,
        OverlayService overlayService,
        InstanceStateService stateService,
        ProfileManager profileManager) : base(context, aggregator, instanceManager, profileManager)
    {
        _notificationService = notificationService;
        _overlayService = overlayService;
        _stateService = stateService;

        var filter = this.WhenValueChanged(x => x.FilterText).Select(BuildFilter);
        _changesSource.Connect().Filter(filter).Bind(out var view).Subscribe().DisposeWith(_subscriptions);
        ChangesView = view;
    }

    #region Direct

    public bool IsLocked => Basic.Source is not null;

    #endregion

    #region Injected

    private readonly NotificationService _notificationService;
    private readonly OverlayService _overlayService;
    private readonly InstanceStateService _stateService;

    #endregion

    #region Fields

    private readonly CompositeDisposable _subscriptions = new();
    private CancellationToken? _initToken;
    private readonly SourceCache<WorkspaceChangeModel, string> _changesSource = new(x => x.RelativePath);

    #endregion

    #region Reactive

    public ReadOnlyObservableCollection<WorkspaceChangeModel> ChangesView { get; }

    public ObservableCollection<ImportBrowserEntryModel> ImportEntries { get; } = [];

    public ObservableCollection<ImportPathSegmentModel> ImportBreadcrumbs { get; } = [];

    [ObservableProperty]
    public partial WorkspaceChangeModel? SelectedChange { get; set; }

    [ObservableProperty]
    public partial ImportBrowserEntryModel? SelectedImportEntry { get; set; }

    [ObservableProperty]
    public partial string FilterText { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentImportDisplayPath))]
    [NotifyPropertyChangedFor(nameof(CurrentImportDirectoryPath))]
    [NotifyPropertyChangedFor(nameof(HasNestedImportPath))]
    public partial string CurrentImportRelativePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int ChangesCount { get; set; }

    [ObservableProperty]
    public partial int ImportEntryCount { get; set; }

    [ObservableProperty]
    public partial bool IsGitRepository { get; set; }

    [ObservableProperty]
    public partial string GitBranchName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string GitHeadSummary { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int GitStagedCount { get; set; }

    [ObservableProperty]
    public partial int GitUnstagedCount { get; set; }

    [ObservableProperty]
    public partial int GitChangedCount { get; set; }

    [ObservableProperty]
    public partial int GitAheadCount { get; set; }

    [ObservableProperty]
    public partial int GitBehindCount { get; set; }

    [ObservableProperty]
    public partial string GitTrackingBranchName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool GitIsDetachedHead { get; set; }

    [ObservableProperty]
    public partial bool GitIsBusy { get; set; }

    public string CurrentImportDisplayPath =>
        string.IsNullOrWhiteSpace(CurrentImportRelativePath)
            ? "import"
            : $"import/{CurrentImportRelativePath.Replace(Path.DirectorySeparatorChar, '/')}";

    public string CurrentImportDirectoryPath => GetCurrentImportDirectoryPath();

    public bool HasNestedImportPath => !string.IsNullOrWhiteSpace(CurrentImportRelativePath);

    #endregion

    #region Overrides

    protected override async Task OnInitializeAsync(CancellationToken token)
    {
        _initToken = token;

        await LoadGitStatusAsync();
        await LoadChangeListAsync(token);
        await LoadImportBrowserAsync();
    }

    protected override Task OnDeinitializeAsync()
    {
        _subscriptions.Dispose();
        return Task.CompletedTask;
    }

    #endregion

    #region Other

    private static Func<WorkspaceChangeModel, bool> BuildFilter(string? text) =>
        x => string.IsNullOrEmpty(text) || x.RelativePath.Contains(text, StringComparison.CurrentCultureIgnoreCase);

    private static string NormalizeRelativePath(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return string.Empty;
        }

        return relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).Trim();
    }

    private string GetImportRootPath()
    {
        var root = PathDef.Default.DirectoryOfImport(Basic.Key);
        Directory.CreateDirectory(root);
        return root;
    }

    private string GetCurrentImportDirectoryPath() =>
        Path.Combine(GetImportRootPath(), NormalizeRelativePath(CurrentImportRelativePath));

    private static IReadOnlyList<ImportPathSegmentModel> BuildImportBreadcrumbs(string relativePath)
    {
        var segments = relativePath.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                                          StringSplitOptions.RemoveEmptyEntries);

        var result = new List<ImportPathSegmentModel>(segments.Length);

        var current = string.Empty;
        for (var i = 0; i < segments.Length; i++)
        {
            current = string.IsNullOrEmpty(current) ? segments[i] : Path.Combine(current, segments[i]);
            result.Add(new() { Label = segments[i], RelativePath = current, IsCurrent = i == segments.Length - 1 });
        }

        return result;
    }

    private async Task LoadImportBrowserAsync()
    {
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(GetImportRootPath()));
        var relativePath = NormalizeRelativePath(CurrentImportRelativePath);
        var currentPath = Path.GetFullPath(Path.Combine(root, relativePath));
        if (!currentPath.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !Directory.Exists(currentPath))
        {
            relativePath = string.Empty;
            currentPath = root;
        }

        var entries = Directory
                     .EnumerateFileSystemEntries(currentPath)
                     .Select(path =>
                      {
                          var isDirectory = Directory.Exists(path);
                          var info = isDirectory ? (FileSystemInfo)new DirectoryInfo(path) : new FileInfo(path);
                          var fullPath = Path.GetFullPath(path);

                          return new ImportBrowserEntryModel
                          {
                              Name = Path.GetFileName(path),
                              RelativePath = Path.GetRelativePath(root, fullPath),
                              FullPath = fullPath,
                              IsDirectory = isDirectory,
                              FileType = isDirectory
                                             ? LanguageManager.Instance.InstanceWorkspacePage_FolderFileTypeText.Current()
                                             :
                                             string.IsNullOrWhiteSpace(Path.GetExtension(path))
                                                 ?
                                                 LanguageManager.Instance.InstanceWorkspacePage_FileFileTypeText.Current()
                                                 : Path.GetExtension(path).TrimStart('.').ToUpperInvariant(),
                              FileSizeRaw = info is FileInfo file ? file.Length : 0,
                              FileLastModifiedRaw = info.LastWriteTime
                          };
                      })
                     .OrderByDescending(entry => entry.IsDirectory)
                     .ThenBy(entry => entry.Name, StringComparer.CurrentCultureIgnoreCase)
                     .ToList();

        var breadcrumbs = BuildImportBreadcrumbs(relativePath);
        var selectedPath = SelectedImportEntry?.RelativePath;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            CurrentImportRelativePath = relativePath;

            ImportEntries.Clear();
            foreach (var entry in entries)
            {
                ImportEntries.Add(entry);
            }

            ImportBreadcrumbs.Clear();
            foreach (var breadcrumb in breadcrumbs)
            {
                ImportBreadcrumbs.Add(breadcrumb);
            }

            ImportEntryCount = ImportEntries.Count;
            SelectedImportEntry = string.IsNullOrWhiteSpace(selectedPath)
                                      ? null
                                      : ImportEntries.FirstOrDefault(entry => string.Equals(entry.RelativePath,
                                                                         selectedPath,
                                                                         StringComparison.OrdinalIgnoreCase));

            RefreshImportCommands();
        });
    }

    private async Task LoadChangeListAsync(CancellationToken token)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _changesSource.Clear();
            ChangesCount = _changesSource.Count;
        });

        var state = await _stateService.RetrieveChangesAsync(Basic.Key, token);
        var batch = state.Entries.Select(entry => new WorkspaceChangeModel
        {
            RelativePath = entry.RelativePath,
            FileName = entry.FileName,
            Kind = entry.Kind,
            LivePath = entry.LivePath,
            ImportPath = entry.ImportPath,
            FileType = entry.FileType,
            FileSizeRaw = entry.FileSize,
            FileLastModifiedRaw = entry.FileLastModified
        }).ToArray();

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _changesSource.AddOrUpdate(batch);
            ChangesCount = _changesSource.Count;
        });
    }

    private async Task LoadGitStatusAsync()
    {
        var state = await _stateService.RetrieveGitAsync(Basic.Key, _initToken ?? CancellationToken.None);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsGitRepository = state.IsRepository;
            GitBranchName = state.BranchName;
            GitHeadSummary = state.HeadSummary;
            GitStagedCount = state.StagedCount;
            GitUnstagedCount = state.UnstagedCount;
            GitChangedCount = state.ChangedCount;
            GitAheadCount = state.AheadCount;
            GitBehindCount = state.BehindCount;
            GitTrackingBranchName = state.TrackingBranchName;
            GitIsDetachedHead = state.IsHeadDetached;
        });

        RefreshGitCommands();
    }

    private bool TryOpenGitRepository(out Repository? repository)
    {
        repository = null;

        var dir = PathDef.Default.DirectoryOfHome(Basic.Key);
        var discoveredPath = Repository.Discover(dir);
        if (string.IsNullOrEmpty(discoveredPath))
        {
            return false;
        }

        var candidate = new Repository(discoveredPath);
        if (!FileHelper.IsPathEquivalent(candidate.Info.WorkingDirectory, dir))
        {
            candidate.Dispose();
            return false;
        }

        repository = candidate;
        return true;
    }

    private void RefreshGitCommands()
    {
        CommitGitCommand.NotifyCanExecuteChanged();
        RestoreGitChangesCommand.NotifyCanExecuteChanged();
    }

    private void RefreshImportCommands()
    {
        HomeImportCommand.NotifyCanExecuteChanged();
        OpenImportBreadcrumbCommand.NotifyCanExecuteChanged();
        EnterImportDirectoryCommand.NotifyCanExecuteChanged();
        RemoveImportEntryCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedImportEntryChanged(ImportBrowserEntryModel? value) => RefreshImportCommands();

    partial void OnCurrentImportRelativePathChanged(string value) => RefreshImportCommands();

    private void SetGitBusy(bool value)
    {
        GitIsBusy = value;
        RefreshGitCommands();
    }

    private async Task RunGitOperationAsync(string errorTitle, Func<Repository, Task> operation)
    {
        if (!TryOpenGitRepository(out var repository))
        {
            await LoadGitStatusAsync();
            _notificationService.PopMessage(LanguageManager.Instance.InstanceWorkspacePage_GitNotRepositoryWarningNotificationMessage.Current(),
                                            LanguageManager.Instance.InstanceWorkspacePage_GitErrorWarningNotificationTitle.Current(),
                                            GrowlLevel.Warning);
            return;
        }

        SetGitBusy(true);
        try
        {
            using var openedRepository = repository
                                      ?? throw new InvalidOperationException("Git repository was not opened.");
            await Task.Run(async () => await operation(openedRepository));
        }
        catch (Exception ex)
        {
            _notificationService.PopMessage(ex, errorTitle);
        }
        finally
        {
            SetGitBusy(false);
            await LoadGitStatusAsync();
        }
    }

    private static void DeleteWorkingTreeEntry(string repositoryRoot, string relativePath)
    {
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(repositoryRoot));
        var fullPath = Path.GetFullPath(Path.Combine(root, relativePath));
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Refused to delete path outside repository root.");
        }

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            TrimEmptyParents(root, Path.GetDirectoryName(fullPath));
            return;
        }

        if (Directory.Exists(fullPath))
        {
            Directory.Delete(fullPath, true);
        }
    }

    private static void TrimEmptyParents(string repositoryRoot, string? current)
    {
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(repositoryRoot));
        while (!string.IsNullOrEmpty(current) && !FileHelper.IsPathEquivalent(current, root))
        {
            if (Directory.EnumerateFileSystemEntries(current).Any())
            {
                break;
            }

            Directory.Delete(current);
            current = Path.GetDirectoryName(current);
        }
    }

    private static string BuildRestoreConfirmationMessage(int unstagedCount) =>
        LanguageManager.Instance.InstanceWorkspacePage_GitRestoreConfirmationMessage.Current().Replace("{0}", unstagedCount.ToString());

    private static bool IsImportProjectionEntity(string path) =>
        File.Exists(path) && File.ResolveLinkTarget(path, false) is null;

    #endregion

    #region Commands

    [RelayCommand]
    private async Task RefreshChangesAsync()
    {
        await LoadChangeListAsync(_initToken ?? CancellationToken.None);
        await LoadGitStatusAsync();
    }

    [RelayCommand]
    private async Task RefreshImportAsync()
    {
        await LoadImportBrowserAsync();
        await LoadGitStatusAsync();
    }

    private bool CanGoImportHome() => !string.IsNullOrWhiteSpace(CurrentImportRelativePath);

    [RelayCommand(CanExecute = nameof(CanGoImportHome))]
    private async Task HomeImportAsync()
    {
        CurrentImportRelativePath = string.Empty;
        await LoadImportBrowserAsync();
    }

    private bool CanOpenImportBreadcrumb(ImportPathSegmentModel? model) => model is { IsCurrent: false };

    [RelayCommand(CanExecute = nameof(CanOpenImportBreadcrumb))]
    private async Task OpenImportBreadcrumbAsync(ImportPathSegmentModel? model)
    {
        if (model == null)
        {
            return;
        }

        CurrentImportRelativePath = model.RelativePath;
        await LoadImportBrowserAsync();
    }

    private bool CanEnterImportDirectory(ImportBrowserEntryModel? model) => model is { IsDirectory: true };

    [RelayCommand(CanExecute = nameof(CanEnterImportDirectory))]
    private async Task EnterImportDirectoryAsync(ImportBrowserEntryModel? model)
    {
        if (model == null)
        {
            return;
        }

        CurrentImportRelativePath = model.RelativePath;
        await LoadImportBrowserAsync();
    }

    private bool CanAddImportEntry() => !IsLocked;

    [RelayCommand(CanExecute = nameof(CanAddImportEntry))]
    private async Task AddImportEntryAsync()
    {
        var filePath = await _overlayService.RequestFileAsync(LanguageManager.Instance.InstanceWorkspacePage_AddImportFilePrompt.Current(),
                                                              LanguageManager.Instance.InstanceWorkspacePage_AddImportFileTitle.Current(),
                                                              GetPreferredImportPickerDirectoryPath());
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return;
        }

        try
        {
            var directory = GetCurrentImportDirectoryPath();
            Directory.CreateDirectory(directory);

            var fileName = Path.GetFileName(filePath);
            var targetPath = Path.Combine(directory, fileName);
            if (FileHelper.IsPathEquivalent(filePath, targetPath))
            {
                _notificationService.PopMessage(LanguageManager.Instance.InstanceWorkspacePage_AddImportFileAlreadyExistsWarningNotificationMessage.Current(),
                                                LanguageManager.Instance.InstanceWorkspacePage_AddImportFileAlreadyExistsWarningNotificationTitle.Current(),
                                                GrowlLevel.Warning);
                return;
            }

            var overwrite = false;
            if (File.Exists(targetPath))
            {
                overwrite =
                    await _overlayService.RequestConfirmationAsync(LanguageManager.Instance.InstanceWorkspacePage_AddImportFileOverwriteConfirmationMessage.Current()
                                                                  .Replace("{0}", fileName),
                                                                   LanguageManager.Instance.InstanceWorkspacePage_AddImportFileOverwriteConfirmationTitle.Current());
                if (!overwrite)
                {
                    return;
                }
            }

            File.Copy(filePath, targetPath, overwrite);
            await RefreshImportAsync();

            SelectedImportEntry = ImportEntries.FirstOrDefault(entry => string.Equals(entry.RelativePath,
                                                                   Path.GetRelativePath(GetImportRootPath(),
                                                                       targetPath),
                                                                   StringComparison.OrdinalIgnoreCase));

            _notificationService.PopMessage(LanguageManager.Instance.InstanceWorkspacePage_AddImportFileSuccessNotificationMessage.Current()
                                                     .Replace("{0}", fileName),
                                            LanguageManager.Instance.InstanceWorkspacePage_AddImportFileSuccessNotificationTitle.Current(),
                                            GrowlLevel.Success);
        }
        catch (Exception ex)
        {
            _notificationService.PopMessage(ex, "添加 import 文件失败");
        }
    }

    private string? GetPreferredImportPickerDirectoryPath()
    {
        var buildRoot = PathDef.Default.DirectoryOfBuild(Basic.Key);
        var currentRelativePath = NormalizeRelativePath(CurrentImportRelativePath);
        var preferredBuildPath = Path.Combine(buildRoot, currentRelativePath);
        if (Directory.Exists(preferredBuildPath))
        {
            return preferredBuildPath;
        }

        if (Directory.Exists(buildRoot))
        {
            return buildRoot;
        }

        var importPath = GetCurrentImportDirectoryPath();
        return Directory.Exists(importPath) ? importPath : null;
    }

    private bool CanRemoveImportEntry(ImportBrowserEntryModel? model) =>
        !IsLocked && (model ?? SelectedImportEntry) is not null;

    [RelayCommand(CanExecute = nameof(CanRemoveImportEntry))]
    private async Task RemoveImportEntryAsync(ImportBrowserEntryModel? model)
    {
        model ??= SelectedImportEntry;
        if (model == null)
        {
            return;
        }

        var confirmed =
            await _overlayService.RequestConfirmationAsync(LanguageManager.Instance.InstanceWorkspacePage_RemoveImportEntryConfirmationMessage.Current()
                                                          .Replace("{0}", model.Name),
                                                           model.IsDirectory
                                                               ? LanguageManager.Instance.InstanceWorkspacePage_RemoveImportDirectoryConfirmationTitle.Current()
                                                               : LanguageManager.Instance.InstanceWorkspacePage_RemoveImportFileConfirmationTitle.Current());
        if (!confirmed)
        {
            return;
        }

        try
        {
            if (model.IsDirectory)
            {
                Directory.Delete(model.FullPath, true);
            }
            else if (File.Exists(model.FullPath))
            {
                File.Delete(model.FullPath);
            }

            SelectedImportEntry = null;
            await RefreshImportAsync();
            _notificationService.PopMessage(LanguageManager.Instance.InstanceWorkspacePage_RemoveImportEntrySuccessNotificationMessage.Current()
                                                     .Replace("{0}", model.Name),
                                            LanguageManager.Instance.InstanceWorkspacePage_RemoveImportEntrySuccessNotificationTitle.Current(),
                                            GrowlLevel.Success);
        }
        catch (Exception ex)
        {
            _notificationService.PopMessage(ex, "移出 import 文件失败");
        }
    }

    private bool CanOpenDiffer(WorkspaceChangeModel? model) => model is not null;

    [RelayCommand(CanExecute = nameof(CanOpenDiffer))]
    private async Task OpenDiffer(WorkspaceChangeModel? model)
    {
        if (model != null)
        {
            if (model.FileSizeRaw > 1024 * 1024 * 1024
             && !await _overlayService.RequestConfirmationAsync(LanguageManager.Instance.InstanceWorkspacePage_LargeDiffConfirmationMessage.Current()))
            {
                return;
            }

            _overlayService.PopModal<WorkspaceDiffModal>(model);
        }
    }

    private bool CanStage(WorkspaceChangeModel? model) =>
        !IsLocked && model is not null && IsImportProjectionEntity(model.LivePath);

    [RelayCommand(CanExecute = nameof(CanStage))]
    private async Task Stage(WorkspaceChangeModel? model)
    {
        if (model == null || !IsImportProjectionEntity(model.LivePath))
        {
            return;
        }

        if (!await _overlayService.RequestConfirmationAsync(LanguageManager.Instance.InstanceWorkspacePage_StageConfirmationMessage.Current()))
        {
            return;
        }

        var suc = false;
        if (File.Exists(model.ImportPath))
        {
            try
            {
                File.Copy(model.LivePath, model.ImportPath, true);
                suc = true;
            }
            catch (Exception ex)
            {
                _notificationService.PopMessage(ex, LanguageManager.Instance.InstanceWorkspacePage_FileStagingDangerNotificationTitle.Current());
            }
        }
        else
        {
            try
            {
                File.Delete(model.LivePath);
                suc = true;
            }
            catch (Exception ex)
            {
                _notificationService.PopMessage(ex, LanguageManager.Instance.InstanceWorkspacePage_FileStagingDangerNotificationTitle.Current());
            }
        }

        if (suc)
        {
            model.Kind = WorkspaceChangeKind.Same;
            await LoadGitStatusAsync();
        }
    }

    private bool CanRestore(WorkspaceChangeModel? model) => model is not null;

    [RelayCommand(CanExecute = nameof(CanRestore))]
    private async Task Restore(WorkspaceChangeModel? model)
    {
        if (model == null)
        {
            return;
        }

        if (!await _overlayService.RequestConfirmationAsync(LanguageManager.Instance.InstanceWorkspacePage_RestoreConfirmationMessage.Current()))
        {
            return;
        }

        var suc = false;
        if (IsImportProjectionEntity(model.LivePath))
        {
            try
            {
                if (File.Exists(model.ImportPath))
                {
                    File.Copy(model.ImportPath, model.LivePath, true);
                }
                else
                {
                    File.Delete(model.LivePath);
                }

                suc = true;
            }
            catch (Exception ex)
            {
                _notificationService.PopMessage(ex, LanguageManager.Instance.InstanceWorkspacePage_FileStagingDangerNotificationTitle.Current());
            }
        }

        if (suc)
        {
            model.Kind = WorkspaceChangeKind.Same;
            await LoadGitStatusAsync();
        }
    }

    private bool CanCommitGit() => IsGitRepository && !GitIsBusy && !GitIsDetachedHead && GitChangedCount > 0;

    [RelayCommand(CanExecute = nameof(CanCommitGit))]
    private async Task CommitGit() =>
        await RunGitOperationAsync(LanguageManager.Instance.InstanceWorkspacePage_GitCommitDangerNotificationTitle.Current(),
                                   async repository =>
                                   {
                                       var signature = repository.Config.BuildSignature(DateTimeOffset.Now);
                                       if (signature is null)
                                       {
                                           _notificationService.PopMessage(LanguageManager.Instance.InstanceWorkspacePage_GitCommitNoIdentityWarningNotificationMessage.Current(),
                                                                           LanguageManager.Instance.InstanceWorkspacePage_GitCommitDangerNotificationTitle.Current(),
                                                                           GrowlLevel.Warning);
                                           return;
                                       }

                                       var message =
                                           await Dispatcher.UIThread.InvokeAsync(async () =>
                                               await _overlayService.RequestInputAsync(LanguageManager.Instance.InstanceWorkspacePage_GitCommitPrompt.Current(),
                                                   LanguageManager.Instance.InstanceWorkspacePage_GitCommitPromptTitle.Current(),
                                                   LanguageManager.Instance.InstanceWorkspacePage_GitCommitDefaultMessage.Current()
                                                            .Replace("{0}", Basic.Name),
                                                   true));
                                       if (message is null)
                                       {
                                           return;
                                       }

                                       var trimmedMessage = message.Trim();
                                       if (string.IsNullOrEmpty(trimmedMessage))
                                       {
                                           _notificationService.PopMessage(LanguageManager.Instance.InstanceWorkspacePage_GitCommitEmptyWarningNotificationMessage.Current(),
                                                                           LanguageManager.Instance.InstanceWorkspacePage_GitCommitDangerNotificationTitle.Current(),
                                                                           GrowlLevel.Warning);
                                           return;
                                       }

                                       var paths = repository
                                                  .RetrieveStatus(new StatusOptions
                                                  {
                                                      IncludeIgnored = false,
                                                      RecurseUntrackedDirs = true
                                                  })
                                                  .Select(entry => entry.FilePath)
                                                  .Distinct(StringComparer.OrdinalIgnoreCase)
                                                  .ToArray();
                                       if (paths.Length == 0)
                                       {
                                           _notificationService.PopMessage(LanguageManager.Instance.InstanceWorkspacePage_GitCommitNoChangesInformationNotificationMessage.Current(),
                                                                           LanguageManager.Instance.InstanceWorkspacePage_GitCommitPromptTitle.Current());
                                           return;
                                       }

                                       Commands.Stage(repository, paths);

                                       var commit = repository.Commit(trimmedMessage, signature, signature);
                                       _notificationService.PopMessage(LanguageManager.Instance.InstanceWorkspacePage_GitCommitSuccessNotificationMessage.Current()
                                                                      .Replace("{0}", commit.Sha[..7]),
                                                                       LanguageManager.Instance.InstanceWorkspacePage_GitCommitSuccessNotificationTitle.Current(),
                                                                       GrowlLevel.Success);
                                   });

    private bool CanRestoreGitChanges() => IsGitRepository && !GitIsBusy && GitStagedCount == 0 && GitUnstagedCount > 0;

    [RelayCommand(CanExecute = nameof(CanRestoreGitChanges))]
    private async Task RestoreGitChanges()
    {
        if (!await _overlayService.RequestConfirmationAsync(BuildRestoreConfirmationMessage(GitUnstagedCount),
                                                            LanguageManager.Instance.InstanceWorkspacePage_GitRestoreConfirmationTitle.Current()))
        {
            return;
        }

        await RunGitOperationAsync(LanguageManager.Instance.InstanceWorkspacePage_GitRestoreDangerNotificationTitle.Current(),
                                   repository =>
                                   {
                                       var status = repository.RetrieveStatus(new StatusOptions
                                       {
                                           IncludeIgnored = false,
                                           RecurseUntrackedDirs = true
                                       });

                                       var trackedPaths = status
                                                         .Where(entry => GitStatusHelper.IsUnstaged(entry.State)
                                                                      && !entry.State.HasFlag(FileStatus.NewInWorkdir))
                                                         .Select(entry => entry.FilePath)
                                                         .Distinct(StringComparer.OrdinalIgnoreCase)
                                                         .ToArray();
                                       if (trackedPaths.Length > 0)
                                       {
                                           if (repository.Head.Tip is null)
                                           {
                                               throw new
                                                   InvalidOperationException("Cannot restore tracked files because HEAD does not exist.");
                                           }

                                           foreach (var path in trackedPaths)
                                           {
                                               Commands.Checkout(repository,
                                                                 repository.Head.Tip.Tree,
                                                                 new() { CheckoutModifiers = CheckoutModifiers.Force },
                                                                 path);
                                           }
                                       }

                                       foreach (var relativePath in status
                                                                   .Where(entry =>
                                                                              entry.State.HasFlag(FileStatus
                                                                                 .NewInWorkdir))
                                                                   .Select(entry => entry.FilePath)
                                                                   .Distinct(StringComparer.OrdinalIgnoreCase)
                                                                   .OrderByDescending(path => path.Length))
                                       {
                                           DeleteWorkingTreeEntry(repository.Info.WorkingDirectory, relativePath);
                                       }

                                       _notificationService.PopMessage(LanguageManager.Instance.InstanceWorkspacePage_GitRestoreSuccessNotificationMessage.Current(),
                                                                       LanguageManager.Instance.InstanceWorkspacePage_GitRestoreSuccessNotificationTitle.Current(),
                                                                       GrowlLevel.Success);
                                       return Task.CompletedTask;
                                   });
    }

    #endregion
}
