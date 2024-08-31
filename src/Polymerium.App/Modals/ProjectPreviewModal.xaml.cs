using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Polymerium.App.Models;
using Polymerium.Trident.Helpers;
using Polymerium.Trident.Services;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Resources;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Polymerium.App.Modals;

public sealed partial class ProjectPreviewModal
{
    // Using a DependencyProperty as the backing store for Profile.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty ProjectProperty =
        DependencyProperty.Register(nameof(Project), typeof(ProjectModel), typeof(ProjectPreviewModal),
            new PropertyMetadata(null));

    private readonly RepositoryAgent _agent;
    private readonly Action<ProjectVersionModel>? _callback;
    private readonly Filter _filter;
    private readonly string _label;
    private readonly LayerModel _layer;
    private readonly string _projectId;
    private readonly Attachment? _reference;

    private readonly CancellationTokenSource tokenSource =
        CancellationTokenSource.CreateLinkedTokenSource(App.Current.Token);

    public ProjectPreviewModal(RepositoryAgent agent, string label, string projectId, Filter filter, LayerModel layer,
        Action<ProjectVersionModel>? callback = null)
    {
        _agent = agent;
        _projectId = projectId;
        _label = label;
        _layer = layer;
        _filter = filter;
        _callback = callback;
        _reference = layer.Attachments.FirstOrDefault(
            x => x.Label == label && x.ProjectId == projectId);
        ;

        InstallCommand = new RelayCommand<ProjectVersionModel>(Install, CanInstall);
        UninstallCommand = new RelayCommand(Uninstall, CanUninstall);
        OpenReferenceCommand = new RelayCommand<Uri>(OpenReference, CanOpenReference);
        InitializeComponent();
    }

    public ProjectModel Project
    {
        get => (ProjectModel)GetValue(ProjectProperty);
        set => SetValue(ProjectProperty, value);
    }

    public IRelayCommand<Uri> OpenReferenceCommand { get; }
    public ICommand InstallCommand { get; }

    public ICommand UninstallCommand { get; }

    private async Task LoadProjectAsync()
    {
        try
        {
            var project = await _agent.QueryAsync(_label, _projectId, tokenSource.Token);
            DispatcherQueue.TryEnqueue(() =>
            {
                Project = new ProjectModel(project, _filter);
                if (_reference != null)
                {
                    var installed = Project.Versions.FirstOrDefault(x => x.Inner.Id == _reference.VersionId);
                    if (installed != null) VersionList.SelectedIndex = Project.Versions.IndexOf(installed);
                }

                VisualStateManager.GoToState(this, "Done", true);
            });
        }
        catch
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                VisualStateManager.GoToState(this, "Failed", true);
            });
        }
    }

    private void ModalBase_Loaded(object sender, RoutedEventArgs e)
    {
        VisualStateManager.GoToState(this, "Fetching", true);
        if (_reference != null)
        {
            VisualStateManager.GoToState(this, "Installed", true);
        }

        Task.Run(LoadProjectAsync);
    }

    private void ModalBase_Unloaded(object sender, RoutedEventArgs e) => tokenSource.Cancel();

    private bool CanOpenReference(Uri? url) => url != null;

    private void OpenReference(Uri? url)
    {
        if (url != null)
        {
            UriFileHelper.OpenInExternal(url.AbsoluteUri);
        }
    }

    //private void MarkdownTextBlock_LinkClicked(object sender, LinkClickedEventArgs e) =>
    //    UriFileHelper.OpenInExternal(e.Link);

    private bool CanInstall(ProjectVersionModel? model) => model != null;

    private void Install(ProjectVersionModel? model)
    {
        if (model != null)
        {
            if (_reference != null)
            {
                _reference.VersionId = model.Inner.Id;
            }
            else
            {
                Attachment attachment = new(model.Root.Inner.Label, model.Root.Inner.Id, model.Inner.Id);
                _layer.Attachments.Add(attachment);
            }

            _callback?.Invoke(model);
            DismissCommand.Execute(null);
        }
    }

    private bool CanUninstall() => _reference != null;

    private void Uninstall()
    {
        if (_reference != null)
        {
            _layer.Attachments.Remove(_reference);
        }
    }
}