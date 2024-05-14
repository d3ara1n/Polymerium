using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml;
using Polymerium.App.Models;
using Polymerium.Trident.Helpers;
using Polymerium.Trident.Services;
using System.Windows.Input;
using Trident.Abstractions.Repositories;
using Trident.Abstractions.Resources;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Polymerium.App.Modals
{
    public sealed partial class ProjectPreviewModal
    {
        // Using a DependencyProperty as the backing store for Profile.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ProjectProperty =
            DependencyProperty.Register(nameof(Project), typeof(ProjectModel), typeof(ProjectPreviewModal),
                new PropertyMetadata(null));

        private readonly RepositoryAgent _agent;
        private readonly Filter _filter;
        private readonly Attachment? _reference;

        private readonly CancellationTokenSource tokenSource =
            CancellationTokenSource.CreateLinkedTokenSource(App.Current.Token);

        public ProjectPreviewModal(ExhibitModel model, RepositoryAgent agent, Filter filter, Attachment? reference,
            ICommand installCommand)
        {
            Exhibit = model;
            _agent = agent;
            _reference = reference;
            _filter = filter;

            InstallCommand = installCommand;
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

        public ExhibitModel Exhibit { get; }

        private async Task LoadProjectAsync()
        {
            try
            {
                var project = await _agent.QueryAsync(Exhibit.Inner.Label, Exhibit.Inner.Id, tokenSource.Token);
                DispatcherQueue.TryEnqueue(() =>
                {
                    Project = new ProjectModel(project, _filter);
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

        private void ModalBase_Unloaded(object sender, RoutedEventArgs e)
        {
            tokenSource.Cancel();
        }

        private bool CanOpenReference(Uri? url)
        {
            return url != null;
        }

        private void OpenReference(Uri? url)
        {
            if (url != null)
            {
                UriFileHelper.OpenInExternal(url.AbsoluteUri);
            }
        }

        private void MarkdownTextBlock_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            UriFileHelper.OpenInExternal(e.Link);
        }
    }
}