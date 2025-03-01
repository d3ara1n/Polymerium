using Avalonia.Controls.Templates;
using Huskui.Avalonia.Controls;
using Polymerium.App.Models;

namespace Polymerium.App.Views;

public partial class ExhibitionWelcomeView : Page
{
    public ExhibitionWelcomeView()
    {
        InitializeComponent();
        if (Resources.TryGetValue("BigNewsDataTemplate", out var big)
         && big is IDataTemplate bigTemplate
         && Resources.TryGetValue("SmallNewsDataTemplate", out var small)
         && small is IDataTemplate smallTemplate)
            NewsContainer.ItemTemplate = new FuncDataTemplate(typeof(MinecraftNewsModel),
                                                              (o, e) => o is MinecraftNewsModel { IsVeryBig: true }
                                                                            ? bigTemplate.Build(o)
                                                                            : smallTemplate.Build(o));
    }
}