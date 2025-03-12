using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using DynamicData;
using Polymerium.App.Models;

namespace Polymerium.App.Components;

public partial class PackageContainer : UserControl
{
    public static readonly DirectProperty<PackageContainer, string> FilterTextProperty =
        AvaloniaProperty.RegisterDirect<PackageContainer, string>(nameof(FilterText),
                                                                  o => o.FilterText,
                                                                  (o, v) => o.FilterText = v);

    public static readonly DirectProperty<PackageContainer, ReadOnlyObservableCollection<InstancePackageModel>?>
        ViewProperty =
            AvaloniaProperty
               .RegisterDirect<PackageContainer, ReadOnlyObservableCollection<InstancePackageModel>?>(nameof(View),
                    o => o.View,
                    (o, v) => o.View = v);

    public static readonly DirectProperty<PackageContainer, SourceCache<InstancePackageModel, string>?> ItemsProperty =
        AvaloniaProperty.RegisterDirect<PackageContainer, SourceCache<InstancePackageModel, string>?>(nameof(Items),
            o => o.Items,
            (o, v) => o.Items = v);

    public static readonly DirectProperty<PackageContainer, int> TotalCountProperty =
        AvaloniaProperty.RegisterDirect<PackageContainer, int>(nameof(TotalCount),
                                                               o => o.TotalCount,
                                                               (o, v) => o.TotalCount = v);

    public static readonly DirectProperty<PackageContainer, ICommand?> OpenUrlCommandProperty =
        AvaloniaProperty.RegisterDirect<PackageContainer, ICommand?>(nameof(OpenUrlCommand),
                                                                     o => o.OpenUrlCommand,
                                                                     (o, v) => o.OpenUrlCommand = v);

    public static readonly DirectProperty<PackageContainer, bool> IsLockedProperty =
        AvaloniaProperty.RegisterDirect<PackageContainer, bool>(nameof(IsLocked),
                                                                o => o.IsLocked,
                                                                (o, v) => o.IsLocked = v);


    private IDisposable? _subscription;

    public PackageContainer() => InitializeComponent();

    public ICommand? OpenUrlCommand
    {
        get;
        set => SetAndRaise(OpenUrlCommandProperty, ref field, value);
    }

    public string FilterText
    {
        get;
        set => SetAndRaise(FilterTextProperty, ref field, value);
    } = string.Empty;

    public ReadOnlyObservableCollection<InstancePackageModel>? View
    {
        get;
        set => SetAndRaise(ViewProperty, ref field, value);
    }

    public SourceCache<InstancePackageModel, string>? Items
    {
        get;
        set
        {
            if (SetAndRaise(ItemsProperty, ref field, value))
            {
                _subscription?.Dispose();
                if (value is not null)
                {
                    var filter = this.GetObservable(FilterTextProperty).Select(BuildFilter);
                    _subscription = value.Connect().Filter(filter).Bind(out var view).Subscribe();
                    View = view;
                }
            }
        }
    }

    public bool IsLocked
    {
        get;
        set => SetAndRaise(IsLockedProperty, ref field, value);
    }

    public int TotalCount
    {
        get;
        set => SetAndRaise(TotalCountProperty, ref field, value);
    }

    private static Func<InstancePackageModel, bool> BuildFilter(string filter) =>
        x => string.IsNullOrEmpty(filter)
          || (x is { Name: { } name, Summary: { } summary }
           && (name.Contains(filter, StringComparison.InvariantCultureIgnoreCase)
            || summary.Contains(filter, StringComparison.InvariantCultureIgnoreCase)));
}