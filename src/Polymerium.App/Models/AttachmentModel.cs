using Polymerium.App.Extensions;
using System;
using System.Windows.Input;
using Trident.Abstractions.Resources;

namespace Polymerium.App.Models
{
    public record AttachmentModel
    {
        private ResourceKind _kind;

        private string _projectName;
        private Uri? _reference;

        private DataLoadingState _state = DataLoadingState.Loading;

        private string _summary;

        private Uri? _thumbnail;

        private string _versionName;

        public AttachmentModel(Attachment inner, LayerModel root, DataLoadingState state, string? projectName,
            string? versionName,
            Uri? thumbnail, string? summary, Uri? reference, ResourceKind? kind, ICommand open, ICommand retry, ICommand modify,
            ICommand delete)
        {
            Inner = inner;
            Root = root;

            _state = state;
            _projectName = projectName ?? string.Empty;
            _versionName = versionName ?? string.Empty;
            _summary = summary ?? string.Empty;
            _thumbnail = thumbnail;
            _reference = reference;
            _kind = kind ?? ResourceKind.Modpack;

            OpenCommand = open;
            RetryCommand = retry;
            ModifyCommand = modify;
            DeleteCommand = delete;

            State = this.ToBindable(x => x._state, (x, v) => x._state = v);
            ProjectName = this.ToBindable(x => x._projectName, (x, v) => x._projectName = v);
            VersionName = this.ToBindable(x => x._versionName, (x, v) => x._versionName = v);
            Summary = this.ToBindable(x => x._summary, (x, v) => x._summary = v);
            Thumbnail = this.ToBindable(x => x._thumbnail, (x, v) => x._thumbnail = v);
            Reference = this.ToBindable(x => x._reference, (x, v) => x._reference = v);
            Kind = this.ToBindable(x => x._kind, (x, v) => x._kind = v);
        }

        public Attachment Inner { get; }
        public LayerModel Root { get; }
        public Bindable<AttachmentModel, DataLoadingState> State { get; }
        public Bindable<AttachmentModel, string> ProjectName { get; }
        public Bindable<AttachmentModel, string> VersionName { get; }
        public Bindable<AttachmentModel, string> Summary { get; }
        public Bindable<AttachmentModel, Uri?> Thumbnail { get; }
        public Bindable<AttachmentModel, Uri?> Reference { get; }
        public Bindable<AttachmentModel, ResourceKind> Kind { get; }

        public ICommand OpenCommand { get; }
        public ICommand RetryCommand { get; }
        public ICommand ModifyCommand { get; }
        public ICommand DeleteCommand { get; }
    }
}