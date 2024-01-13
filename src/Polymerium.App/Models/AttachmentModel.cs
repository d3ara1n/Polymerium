using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using PackageUrl;
using Polymerium.App.Helpers;
using Polymerium.Trident.Services;
using Trident.Abstractions;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Resources;

namespace Polymerium.App.Models
{
    public record AttachmentModel
    {
        public RepositoryService Service { get; }
        public DispatcherQueue Dispatcher { get; }
        public string Inner { get; }

        private DataLoadingState state = DataLoadingState.Loading;
        public Bindable<AttachmentModel, DataLoadingState> State { get; }
        private string label = String.Empty;
        public Bindable<AttachmentModel, string> Label { get; }
        private string projectId = string.Empty;
        public Bindable<AttachmentModel, string> ProjectId { get; }
        private string projectName = string.Empty;
        public Bindable<AttachmentModel, string> ProjectName { get; }
        private string versionId = String.Empty;
        public Bindable<AttachmentModel, string> VersionId { get; }
        private string versionName = string.Empty;
        public Bindable<AttachmentModel, string> VersionName { get; }
        private string thumbnail = string.Empty;
        public Bindable<AttachmentModel, string> Thumbnail { get; }
        private string summary = string.Empty;
        public Bindable<AttachmentModel, string> Summary { get; }
        private ResourceKind kind = ResourceKind.Mod;
        public Bindable<AttachmentModel, ResourceKind> Kind { get; }
        private Uri reference = new("https://example.com", UriKind.Absolute);
        public Bindable<AttachmentModel, Uri> Reference { get; }
        public AttachmentModel(RepositoryService service, DispatcherQueue dispatcher, string inner)
        {
            Service = service;
            Dispatcher = dispatcher;
            Inner = inner;

            State = new(this, x => x.state, (x, v) => x.state = v);
            Label = new(this, x => x.label, (x, v) => x.label = v);
            ProjectId = new(this, x => x.projectId, (x, v) => x.projectId = v);
            ProjectName = new(this, x => x.projectName, (x, v) => x.projectName = v);
            VersionId = new(this, x => x.versionId, (x, v) => x.versionId = v);
            VersionName = new(this, x => x.versionName, (x, v) => x.versionName = v);
            Thumbnail = new(this, x => x.thumbnail, (x, v) => x.thumbnail = v);
            Summary = new(this, x => x.summary, (x, v) => x.summary = v);
            Kind = new(this, x => x.kind, (x, v) => x.kind = v);
            Reference = new(this, x => x.reference, (x, v) => x.reference = v);

            Fetch();
        }

        public void Fetch() => Task.Run(FetchAsync);

        public async Task FetchAsync()
        {
            if (state != DataLoadingState.Loading) return;
            if (PurlHelper.TryParse(Inner, out var result) && result.HasValue)
            {
                var resolved = await Service.ResolveAsync(result.Value.Item1, result.Value.Item2, result.Value.Item3);
                if (resolved.IsSuccessful)
                    Dispatcher.TryEnqueue(() =>
                    {
                        Label.Value = result.Value.Item1;
                        ProjectId.Value = resolved.Value.ProjectId;
                        ProjectName.Value = resolved.Value.ProjectName;
                        VersionId.Value = resolved.Value.VersionId;
                        VersionName.Value = resolved.Value.VersionName;
                        Thumbnail.Value = resolved.Value.Thumbnail?.AbsoluteUri ?? AssetPath.PLACEHOLDER_DEFAULT_DIRT;
                        Summary.Value = resolved.Value.Summary;
                        Kind.Value = resolved.Value.Kind;
                        Reference.Value = resolved.Value.Reference;

                        State.Value = DataLoadingState.Done;
                    });
                else
                    Dispatcher.TryEnqueue(() => State.Value = DataLoadingState.Failed);
            }
            else
                Dispatcher.TryEnqueue(() => State.Value = DataLoadingState.Failed);
        }
    }
}
