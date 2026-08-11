using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Polymerium.Avalonia.Facilities;
using Polymerium.Avalonia.Models;
using Polymerium.Avalonia.Utilities;

namespace Polymerium.Avalonia.DialogModels;

// NOTE: 项目唯一的在线导入入口。Trident 库级 Import 只消费本地 zip，
//  而用户侧 Import 同时覆盖在线引用与本地文件，由此对话框统一；
//  分类是本地且无网络的——pref:// 直解析为 PackageIdentifier，http(s) 以 Uri 交给
//  Trident 的 RepositoryAgent.RecognizeAsync。
public partial class ModpackImporterDialogModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ValidateCommand))]
    public partial string? Input { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ResolvedText))]
    public partial ModpackImporterResult? Result { get; private set; }

    [ObservableProperty]
    public partial string? Hint { get; private set; }

    public string? ResolvedText =>
        Result switch
        {
            ModpackImporterResult.File f => f.Path,
            ModpackImporterResult.Pref p => p.Identifier.ToString(),
            ModpackImporterResult.Uri u => u.Value.ToString(),
            _ => null
        };

    public bool CanValidate => !string.IsNullOrWhiteSpace(Input);

    // NOTE: 编辑输入会使已分类结果失效，须重新校验。
    partial void OnInputChanged(string? value)
    {
        Result = null;
        Hint = null;
    }

    [RelayCommand(CanExecute = nameof(CanValidate))]
    private void Validate()
    {
        var input = Input?.Trim();
        if (string.IsNullOrEmpty(input))
        {
            return;
        }

        Result = ModpackUrlDetectionHelper.Detect(input);
        Hint = Result is null ? LanguageManager.Instance.ModpackImporterDialog_UnrecognizedHint.Current() : null;
    }
}
