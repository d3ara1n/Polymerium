using System;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Polymerium.App.Extensions;
using Polymerium.App.Helpers;
using Polymerium.Trident.Services;
using Trident.Abstractions.Resources;

namespace Polymerium.App.Models;

public record AttachmentModel
{
    private ResourceKind kind = ResourceKind.Mod;
    private string label = string.Empty;
    private string projectId = string.Empty;
    private string projectName = string.Empty;
    private Uri reference = new("https://example.com", UriKind.Absolute);

    private DataLoadingState state = DataLoadingState.Loading;
    private string summary = string.Empty;
    private string thumbnail = string.Empty;
    private string versionId = string.Empty;
    private string versionName = string.Empty;

    public AttachmentModel(RepositoryService service, DispatcherQueue dispatcher, string inner, WorkpieceModel root)
    {
        Service = service;
        Dispatcher = dispatcher;
        Inner = inner;
        Root = root;

        State = new Bindable<AttachmentModel, DataLoadingState>(this, x => x.state, (x, v) => x.state = v);
        Label = new Bindable<AttachmentModel, string>(this, x => x.label, (x, v) => x.label = v);
        ProjectId = new Bindable<AttachmentModel, string>(this, x => x.projectId, (x, v) => x.projectId = v);
        ProjectName = new Bindable<AttachmentModel, string>(this, x => x.projectName, (x, v) => x.projectName = v);
        VersionId = new Bindable<AttachmentModel, string>(this, x => x.versionId, (x, v) => x.versionId = v);
        VersionName = new Bindable<AttachmentModel, string>(this, x => x.versionName, (x, v) => x.versionName = v);
        Thumbnail = new Bindable<AttachmentModel, string>(this, x => x.thumbnail, (x, v) => x.thumbnail = v);
        Summary = new Bindable<AttachmentModel, string>(this, x => x.summary, (x, v) => x.summary = v);
        Kind = new Bindable<AttachmentModel, ResourceKind>(this, x => x.kind, (x, v) => x.kind = v);
        Reference = new Bindable<AttachmentModel, Uri>(this, x => x.reference, (x, v) => x.reference = v);

        Fetch();
    }

    public RepositoryService Service { get; }
    public DispatcherQueue Dispatcher { get; }
    public string Inner { get; }
    public WorkpieceModel Root { get; }
    public Bindable<AttachmentModel, DataLoadingState> State { get; }
    public Bindable<AttachmentModel, string> Label { get; }
    public Bindable<AttachmentModel, string> ProjectId { get; }
    public Bindable<AttachmentModel, string> ProjectName { get; }
    public Bindable<AttachmentModel, string> VersionId { get; }
    public Bindable<AttachmentModel, string> VersionName { get; }
    public Bindable<AttachmentModel, string> Thumbnail { get; }
    public Bindable<AttachmentModel, string> Summary { get; }
    public Bindable<AttachmentModel, ResourceKind> Kind { get; }
    public Bindable<AttachmentModel, Uri> Reference { get; }

    public void Fetch()
    {
        Task.Run(FetchAsync);
    }

    public async Task FetchAsync()
    {
        if (PurlHelper.TryParse(Inner, out var result) && result.HasValue)
        {
            var resolved = await Service.ResolveAsync(result.Value.Item1, result.Value.Item2, result.Value.Item3,
                Root.Inner.ExtractFilter());
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
        {
            Dispatcher.TryEnqueue(() => State.Value = DataLoadingState.Failed);
        }
    }
}