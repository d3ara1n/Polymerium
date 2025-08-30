using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.Templates;

namespace Polymerium.App.Widgets;

public abstract class WidgetBase : AvaloniaObject
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<WidgetBase, string>(nameof(Title), string.Empty);


    public static readonly DirectProperty<WidgetBase, IDataTemplate?> FullTemplateProperty =
        AvaloniaProperty.RegisterDirect<WidgetBase, IDataTemplate?>(nameof(FullTemplate),
                                                                    o => o.FullTemplate,
                                                                    (o, v) => o.FullTemplate = v);

    public static readonly DirectProperty<WidgetBase, IDataTemplate?> SlimTemplateProperty =
        AvaloniaProperty.RegisterDirect<WidgetBase, IDataTemplate?>(nameof(SlimTemplate),
                                                                    o => o.SlimTemplate,
                                                                    (o, v) => o.SlimTemplate = v);

    public static readonly DirectProperty<WidgetBase, bool> IsPinnedProperty =
        AvaloniaProperty.RegisterDirect<WidgetBase, bool>(nameof(IsPinned),
                                                          o => o.IsPinned,
                                                          (o, v) => o.IsPinned = v);

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public IDataTemplate? FullTemplate
    {
        get;
        set => SetAndRaise(FullTemplateProperty, ref field, value);
    }

    public bool IsPinned
    {
        get => Context.IsPinned;
        set
        {
            if (value != Context.IsPinned)
            {
                Context.IsPinned = value;
                RaisePropertyChanged(IsPinnedProperty, !value, value);
            }
        }
    }


    public IDataTemplate? SlimTemplate
    {
        get;
        set => SetAndRaise(SlimTemplateProperty, ref field, value);
    }

    public required WidgetContext Context { get; set; }


    public Task InitializeAsync() => OnInitializeAsync();
    public Task DeinitializeAsync() => OnDeinitializeAsync();
    protected virtual Task OnDeinitializeAsync() => Task.CompletedTask;
    protected virtual Task OnInitializeAsync() => Task.CompletedTask;
}
