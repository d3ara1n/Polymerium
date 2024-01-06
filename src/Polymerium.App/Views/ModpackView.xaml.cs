using HtmlAgilityPack;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Polymerium.App.Helpers;
using Polymerium.App.ViewModels;
using System;
using System.Linq;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Polymerium.App.Views
{
    public sealed partial class ModpackView : Page
    {
        public ModpackViewModel ViewModel { get; } = App.ViewModel<ModpackViewModel>();

        public ModpackView()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ViewModel.OnAttached(e.Parameter);
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.Project) && ViewModel.Project != null)
            {
                DescriptionView.Blocks.Clear();
                var html = ViewModel.Project.Inner.DescriptionHtml.Replace("&nbsp;", " ");
                var doc = new HtmlDocument();
                doc.LoadHtml(html);
                var paragraph = new Paragraph();
                HtmlHelper.ParseNodeToInline(paragraph.Inlines, doc.DocumentNode, ViewModel.Project.Inner.Reference.AbsoluteUri);
                DescriptionView.Blocks.Add(paragraph);
            }
        }

        
    }
}
