using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Polymerium.App.Models;
using Polymerium.Trident.Services;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Trident.Abstractions.Resources;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Polymerium.App.Modals
{
    public sealed partial class ProjectPreviewModal
    {
        // Using a DependencyProperty as the backing store for Model.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ProjectProperty =
            DependencyProperty.Register(nameof(Project), typeof(ModpackModel), typeof(ProjectPreviewModal),
                new PropertyMetadata(null));

        private readonly RepositoryAgent _agent;

        private readonly Attachment? reference;

        private readonly CancellationTokenSource tokenSource =
            CancellationTokenSource.CreateLinkedTokenSource(App.Current.Token);

        public ProjectPreviewModal(ExhibitModel model, RepositoryAgent agent, Attachment? installed,
            ICommand installCommand, ICommand uninstallCommand)
        {
            _agent = agent;
            Exhibit = model;
            reference = installed;

            OpenReferenceCommand = new RelayCommand<Uri>(OpenReference, CanOpenReference);
            InitializeComponent();
        }

        public ModpackModel Project
        {
            get => (ModpackModel)GetValue(ProjectProperty);
            set => SetValue(ProjectProperty, value);
        }

        public IRelayCommand<Uri> OpenReferenceCommand { get; }

        public ExhibitModel Exhibit { get; }

        private async Task LoadProjectAsync()
        {
            Project? project = await _agent.QueryAsync(Exhibit.Inner.Label, Exhibit.Inner.Id, tokenSource.Token);
            DispatcherQueue.TryEnqueue(() =>
            {
                if (project != null)
                {
                    Project = new ModpackModel(project);
                    VisualStateManager.GoToState(this, "Done", true);
                }
                else
                {
                    VisualStateManager.GoToState(this, "Failed", true);
                }
            });
        }

        private void ModalBase_Loaded(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, "Fetching", true);
            if (reference != null)
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
                Process.Start(new ProcessStartInfo(url.AbsoluteUri) { UseShellExecute = true });
            }
        }
    }
}