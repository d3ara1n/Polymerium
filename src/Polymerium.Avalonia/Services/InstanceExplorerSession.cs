using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.Input;
using Polymerium.Avalonia.Assets;
using Polymerium.Avalonia.Exceptions;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Modals;
using Polymerium.Avalonia.Pages;
using Polymerium.Avalonia.Properties;
using Polymerium.Avalonia.Utilities;
using TridentCore.Abstractions.FileModels;
using TridentCore.Abstractions.Repositories;
using TridentCore.Abstractions.Repositories.Resources;
using TridentCore.Abstractions.Utilities;
using TridentCore.Core.Services;
using TridentCore.Pref;

namespace Polymerium.Avalonia.Services;

public sealed class InstanceExplorerSession : ExplorerSession
{
    private readonly string _key;
    private readonly ProfileManager _profileManager;
    private readonly DataService _dataService;
    private readonly OverlayService _overlayService;
    private readonly PersistenceService _persistenceService;
    private InstanceBasicModel? _basic;
    private Profile? _profile;

    public InstanceExplorerSession(
        string key,
        ProfileManager profileManager,
        DataService dataService,
        OverlayService overlayService,
        PersistenceService persistenceService)
    {
        _key = key;
        _profileManager = profileManager;
        _dataService = dataService;
        _overlayService = overlayService;
        _persistenceService = persistenceService;
    }

    public override void Validate()
    {
        if (!_profileManager.TryGetImmutable(_key, out var profile))
        {
            throw new PageNotReachedException(typeof(ExplorerPage),
                                              Resources.InstancePage_KeyNotFoundExceptionMessage.Replace("{0}", _key));
        }

        _profile = profile;
        _basic = new(_key, profile.Name, profile.Setup.Version, profile.Setup.Loader, profile.Setup.Source);
    }

    public override string Title => _basic?.Name ?? _key;

    public override Bitmap? Background => _basic?.Thumbnail;

    public override Filter? InitialFilter =>
        _basic is null
            ? null
            : new(_basic.Version,
                  _basic.Loader != null && LoaderHelper.TryParse(_basic.Loader, out var loader) ? loader.Identity : null,
                  null);

    #region Exhibit construction

    public override ExhibitModel BuildExhibit(Exhibit hit) =>
        CreateModel(hit.Label,
                    hit.Namespace,
                    hit.Pid,
                    hit.Name,
                    hit.Summary,
                    hit.Thumbnail ?? AssetUriIndex.DirtImage,
                    hit.Author,
                    hit.Tags,
                    hit.UpdatedAt,
                    hit.DownloadCount,
                    hit.Reference,
                    FindEntry(hit.Label, hit.Namespace, hit.Pid));

    // NOTE: instance 弹窗的依赖列表用这个把一个 Project 包回携带 Entry 的模型，并优先复用
    //  待定区/搜索结果里已存在的实例，避免同一项目出现两份模型导致卡片状态失步。
    //  它是 instance 的内部事务——explorer 和基类契约都不认识它。
    private InstanceExhibitModel LinkExhibit(Project project, Func<ProjectIdentifier, ExhibitModel?> findExisting)
    {
        var identifier = new ProjectIdentifier(project.Label, project.Namespace, project.ProjectId);
        if (findExisting(identifier) is { } existing)
        {
            return (InstanceExhibitModel)existing;
        }

        return CreateModel(project.Label,
                           project.Namespace,
                           project.ProjectId,
                           project.ProjectName,
                           project.Summary,
                           project.Thumbnail ?? AssetUriIndex.DirtImage,
                           project.Author,
                           project.Tags,
                           project.UpdatedAt,
                           project.DownloadCount,
                           project.Reference,
                           FindEntry(project.Label, project.Namespace, project.ProjectId));
    }

    private InstanceExhibitModel CreateModel(string label,
                                             string? ns,
                                             string projectId,
                                             string projectName,
                                             string summary,
                                             Uri thumbnail,
                                             string author,
                                             IReadOnlyList<string> tags,
                                             DateTimeOffset updatedAt,
                                             ulong downloads,
                                             Uri reference,
                                             Profile.Rice.Entry? entry)
    {
        var model = new InstanceExhibitModel(label,
                                             ns,
                                             projectId,
                                             projectName,
                                             summary,
                                             thumbnail,
                                             author,
                                             tags,
                                             updatedAt,
                                             downloads,
                                             reference)
        {
            Entry = entry,
            IsFavorite = _persistenceService.IsFavoriteProject(label, ns, projectId)
        };
        StampInstalled(model, entry);
        return model;
    }

    // HACK: 为了优化性能，这里不获取 VersionName，而是在弹窗弹出前加载数据时一并获取
    private void StampInstalled(InstanceExhibitModel model, Profile.Rice.Entry? entry)
    {
        model.State = ResolveState(entry);
        if (entry is not null && PackageHelper.TryParse(entry.Pref, out var parsed))
        {
            model.InstalledVersionId = parsed.Version;
        }
    }

    private ExhibitState? ResolveState(Profile.Rice.Entry? entry) =>
        entry is null
            ? null
            : PackageSourceHelper.CanUpdate(entry.Source, _basic!.Source)
                ? ExhibitState.Editable
                : ExhibitState.Locked;

    public override void RevertState(ExhibitModel exhibit) =>
        exhibit.State = ResolveState((exhibit as InstanceExhibitModel)?.Entry);

    private Profile.Rice.Entry? FindEntry(string label, string? @namespace, string projectId) =>
        _profile?.Setup.Packages.FirstOrDefault(y => PackageHelper.IsMatched(y.Pref, label, @namespace, projectId));

    #endregion

    #region View

    public override async Task ViewExhibitAsync(ExhibitModel exhibit,
                                                Action<ExhibitModel> modifyPending,
                                                Func<ProjectIdentifier, ExhibitModel?> findExisting)
    {
        var project = await _dataService.QueryProjectAsync(new(exhibit.Label, exhibit.Namespace, exhibit.ProjectId));

        if (exhibit.InstalledVersionId != null)
        {
            var package = await _dataService.ResolvePackageAsync(new(exhibit.Label,
                                                                     exhibit.Namespace,
                                                                     exhibit.ProjectId,
                                                                     exhibit.InstalledVersionId),
                                                                 Filter.None);
            exhibit.InstalledVersionName = package.VersionName;
        }

        var model = new ExhibitPackageModel(exhibit.Label,
                                            exhibit.Namespace,
                                            exhibit.ProjectId,
                                            project.ProjectName,
                                            project.Author,
                                            project.Reference,
                                            project.Thumbnail,
                                            project.Tags,
                                            project.DownloadCount,
                                            project.Summary,
                                            project.UpdatedAt,
                                            [.. project.Gallery.Select(x => x.Url)]);

        _overlayService.PopModal(new ExhibitPackageModal
        {
            Key = _key,
            PersistenceService = _persistenceService,
            DataContext = model,
            Exhibit = exhibit,
            DataService = _dataService,
            Filter = new(_basic!.Version,
                         _basic.Loader != null && LoaderHelper.TryParse(_basic.Loader, out var loader)
                             ? loader.Identity
                             : null,
                         project.Kind),
            ModifyPendingCallback = modifyPending,
            UndoCallback = m =>
            {
                RevertState(m);
                modifyPending(m);
            },
            ViewPackageCommand = new AsyncRelayCommand<ExhibitModel>(m => ViewExhibitAsync(m!, modifyPending, findExisting)),
            LinkExhibitCallback = project => LinkExhibit(project, findExisting)
        });
    }

    #endregion

    #region Collect

    // NOTE: Entry 是 Validate 时捕获的同一份 Profile 里的对象，TryGetMutable 的 guard 包的也是这份
    //  Profile，所以直接改 entry.Pref / Remove(entry) 就能落盘，零 lookup。
    public override async Task<bool> CollectAsync(IReadOnlyList<ExhibitModel> pending)
    {
        if (!_profileManager.TryGetMutable(_key, out var guard))
        {
            return false;
        }

        foreach (var model in pending)
        {
            switch (model)
            {
                case InstanceExhibitModel { State: ExhibitState.Adding } m:
                {
                    var entry = new Profile.Rice.Entry
                    {
                        Enabled = true,
                        Pref = PackageHelper.ToPref(m.Label, m.Namespace, m.ProjectId, m.PendingVersionId),
                        Source = null
                    };
                    _persistenceService.AppendAction(new()
                    {
                        Key = _key,
                        Kind = PersistenceService.ActionKind.EditPackage,
                        New = entry.Pref
                    });
                    guard.Value.Setup.Packages.Add(entry);
                    m.State = ExhibitState.Editable;
                    m.Entry = entry;
                    m.InstalledVersionName = m.PendingVersionName;
                    m.InstalledVersionId = m.PendingVersionId;
                    break;
                }
                case InstanceExhibitModel { State: ExhibitState.Removing } m when m.Entry is not null:
                {
                    var old = m.Entry.Pref;
                    guard.Value.Setup.Packages.Remove(m.Entry);
                    _persistenceService.AppendAction(new()
                    {
                        Key = _key,
                        Kind = PersistenceService.ActionKind.EditPackage,
                        Old = old
                    });
                    m.State = null;
                    m.Entry = null;
                    m.InstalledVersionName = null;
                    m.InstalledVersionId = null;
                    break;
                }
                case InstanceExhibitModel { State: ExhibitState.Modifying } m when m.Entry is not null:
                {
                    var old = m.Entry.Pref;
                    m.Entry.Pref = PackageHelper.ToPref(m.Label, m.Namespace, m.ProjectId, m.PendingVersionId);
                    _persistenceService.AppendAction(new()
                    {
                        Key = _key,
                        Kind = PersistenceService.ActionKind.EditPackage,
                        Old = old,
                        New = m.Entry.Pref
                    });
                    m.State = ExhibitState.Editable;
                    m.InstalledVersionName = m.PendingVersionName;
                    m.InstalledVersionId = m.PendingVersionId;
                    break;
                }
            }
        }

        await guard.DisposeAsync();
        return true;
    }

    #endregion
}
