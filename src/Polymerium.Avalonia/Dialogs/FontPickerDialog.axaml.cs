using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Polymerium.Avalonia.Models;

namespace Polymerium.Avalonia.Dialogs;

public partial class FontPickerDialog : Dialog
{
    private List<string> _allSystemFonts = [];
    private FontFamily _fallback = new("sans-serif");

    public FontPickerDialog() => InitializeComponent();

    // 列表数据源：实例不变，仅填充/清空内容，ObservableCollection 的增删通知 ListBox 更新。
    public ObservableCollection<string> FilteredSystemFonts { get; } = [];

    public string? SearchText
    {
        get;
        set => SetAndRaise(SearchTextProperty, ref field, value);
    }

    public string? SelectedSystemFont
    {
        get;
        set => SetAndRaise(SelectedSystemFontProperty, ref field, value);
    }

    public FontModelBase? Selected
    {
        get;
        set => SetAndRaise(SelectedProperty, ref field, value);
    }

    protected override bool ValidateResult(object? result) => true;

    public void Initialize(FontModelBase current, FontFamily fallback)
    {
        _fallback = fallback;
        _allSystemFonts = [.. FontModelBase.SystemFontFamilies.OrderBy(n => n)];
        Selected = current;
        if (current is SystemFontModel { IsAvailable: true } sys)
        {
            SelectedSystemFont = sys.FamilyName;
        }

        RefreshFiltered();
    }

    private void RefreshFiltered()
    {
        FilteredSystemFonts.Clear();
        var query = (SearchText ?? string.Empty).Trim();
        foreach (var family in _allSystemFonts)
        {
            if (query.Length == 0 || family.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                FilteredSystemFonts.Add(family);
            }
        }
    }

    // 属性变化的副作用统一在此处理，保持各属性 setter 只剩 SetAndRaise。
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SearchTextProperty)
        {
            RefreshFiltered();
        }
        else if (change.Property == SelectedSystemFontProperty && SelectedSystemFont is { } name)
        {
            Selected = FontModelBase.FromSystem(name, _fallback);
        }
        else if (change.Property == SelectedProperty)
        {
            Result = Selected;
        }
    }

    [RelayCommand]
    private void UseDefault()
    {
        Selected = new DefaultFontModel(_fallback);
        SelectedSystemFont = null;
    }

    [RelayCommand]
    private async Task ChooseFileAsync()
    {
        var storage = TopLevel.GetTopLevel(this)?.StorageProvider;
        if (storage is null)
        {
            return;
        }

        var result = await storage.OpenFilePickerAsync(new()
        {
            Title = Properties.Resources
                              .FontPickerDialog_ChooseFileTitle,
            FileTypeFilter =
            [
                new(Properties.Resources
                              .FontPickerDialog_FontFileFilter)
                {
                    Patterns = ["*.ttf", "*.otf", "*.ttc"]
                }
            ],
            AllowMultiple = false
        });

        if (result.Count > 0 && result[0].TryGetLocalPath() is { } path)
        {
            Selected = FontModelBase.FromFile(path, _fallback);
            SelectedSystemFont = null;
        }
    }

    #region Avalonia Properties

    public static readonly DirectProperty<FontPickerDialog, FontModelBase?> SelectedProperty =
        AvaloniaProperty.RegisterDirect<FontPickerDialog, FontModelBase?>(nameof(Selected),
                                                                          o => o.Selected,
                                                                          (o, v) => o.Selected = v);

    public static readonly DirectProperty<FontPickerDialog, string?> SearchTextProperty =
        AvaloniaProperty.RegisterDirect<FontPickerDialog, string?>(nameof(SearchText),
                                                                   o => o.SearchText,
                                                                   (o, v) => o.SearchText = v);

    public static readonly DirectProperty<FontPickerDialog, string?> SelectedSystemFontProperty =
        AvaloniaProperty.RegisterDirect<FontPickerDialog, string?>(nameof(SelectedSystemFont),
                                                                   o => o.SelectedSystemFont,
                                                                   (o, v) => o.SelectedSystemFont = v);

    #endregion
}
