using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Media.Imaging;
using Polymerium.App.Modals;
using Polymerium.App.Models;
using Polymerium.App.Services;
using Polymerium.Trident.Extensions;
using Polymerium.Trident.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Resources;

namespace Polymerium.App.ViewModels
{
    public class WorkbenchViewModel : ViewModelBase
    {
        private readonly ModalService _modalService;
        private readonly RepositoryAgent _repositoryAgent;
        private readonly ThumbnailSaver _thumbnailSaver;
        private BitmapImage? background;
        private Filter baseFilter = Filter.EMPTY;
        private LayerModel? model;

        private IncrementalLoadingCollection<IncrementalFactorySource<ExhibitModel>, ExhibitModel>? results;

        public WorkbenchViewModel(ThumbnailSaver thumbnailSaver, RepositoryAgent repositoryAgent,
            ModalService modalService)
        {
            _thumbnailSaver = thumbnailSaver;
            _modalService = modalService;
            _repositoryAgent = repositoryAgent;

            OpenResourceModalCommand = new RelayCommand<ExhibitModel>(OpenResourceModal);
            InstallAttachmentCommand = new RelayCommand<ProjectVersionModel>(InstallAttachment, CanInstallAttachment);
            UninstallAttachmentCommand =
                new RelayCommand<TrackedProjectVersionModel>(UninstallAttachment, CanUninstallAttachment);
        }

        public LayerModel? Model
        {
            get => model;
            set => SetProperty(ref model, value);
        }

        public BitmapImage? Background
        {
            get => background;
            set => SetProperty(ref background, value);
        }

        public IncrementalLoadingCollection<IncrementalFactorySource<ExhibitModel>, ExhibitModel>? Results
        {
            get => results;
            set => SetProperty(ref results, value);
        }

        public ICommand OpenResourceModalCommand { get; }
        private ICommand InstallAttachmentCommand { get; }
        private ICommand UninstallAttachmentCommand { get; }

        public ObservableCollection<TrackedProjectVersionModel> Tracked { get; } = new();

        public override bool OnAttached(object? maybeLayer)
        {
            if (maybeLayer is LayerModel layer)
            {
                Model = layer;
                string? path = _thumbnailSaver.Get(layer.Root.Key);
                if (path != null && File.Exists(path))
                {
                    Background = new BitmapImage(new Uri(path));
                }

                baseFilter = layer.Root.Inner.Metadata.ExtractFilter();
                return true;
            }

            return false;
        }

        public void UpdateSource(string label, string query, ResourceKind kind)
        {
            Results = new IncrementalLoadingCollection<IncrementalFactorySource<ExhibitModel>, ExhibitModel>(
                new IncrementalFactorySource<ExhibitModel>(async (page, limit, token) =>
                    (await _repositoryAgent.SearchAsync(label, query, page, limit, baseFilter with { Kind = kind },
                        token))
                    .Select(ToModel)), 10);
        }

        private void OpenResourceModal(ExhibitModel? exhibit)
        {
            if (exhibit != null)
            {
                Attachment? installed =
                    Model?.Root.Inner.Metadata.Layers.SelectMany(x => x.Attachments).FirstOrDefault(
                        x => x.Label == exhibit.Inner.Label && x.ProjectId == exhibit.Inner.Id);
                ProjectPreviewModal modal = new(exhibit, _repositoryAgent,
                    Model?.Root.Inner.Metadata.ExtractFilter() ?? Filter.EMPTY, installed,
                    InstallAttachmentCommand);
                _modalService.Pop(modal);
            }
        }

        private ExhibitModel ToModel(Exhibit exhibit)
        {
            bool added = Model?.Root.Inner.Metadata.Layers.SelectMany(x => x.Attachments)
                .Any(x => x.Label == exhibit.Label && x.ProjectId == exhibit.Id) ?? false;
            ExhibitModel result = new(exhibit, OpenResourceModalCommand);
            result.HasAdded.Value = added;
            return result;
        }

        private bool CanInstallAttachment(ProjectVersionModel? version)
        {
            return Model != null && version != null;
        }

        private void InstallAttachment(ProjectVersionModel? version)
        {
            if (Model != null && version != null)
            {
                Attachment attachment = new(version.Root.Inner.Label, version.Root.Inner.Id, version.Inner.Id);
                Model.Attachments.Add(attachment);
                TrackedProjectVersionModel tracked = new(version.Inner, version.Root, UninstallAttachmentCommand);
                Tracked.Add(tracked);
                _modalService.Dimiss();
            }
        }

        private bool CanUninstallAttachment(TrackedProjectVersionModel? version)
        {
            return Model != null && version != null && Tracked.Contains(version);
        }

        private void UninstallAttachment(TrackedProjectVersionModel? version)
        {
            if (Model != null && version != null && Tracked.Contains(version))
            {
                // 不比较版本(x.VersionId==null||x.VersionId==version.Inner.Id)，只要保证最后这个项目被移除即可
                Attachment? found = Model.Attachments.FirstOrDefault(x =>
                    x.Label == version.Root.Inner.Label && x.ProjectId == version.Root.Inner.Id);
                if (found != null)
                {
                    Model.Attachments.Remove(found);
                }

                Tracked.Remove(version);
            }
        }
    }
}