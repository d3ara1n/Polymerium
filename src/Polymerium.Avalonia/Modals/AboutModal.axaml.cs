using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;
using Polymerium.Avalonia.Utilities;

namespace Polymerium.Avalonia.Modals;

public partial class AboutModal : Modal
{
    public AboutModal() => InitializeComponent();

    #region Properties

    // 这些值在构造时确定且永不变，get-only 属性即可被绑定读取一次
    public string Brand => Program.Brand;
    public string Version => Program.Version;
    public string ReleaseDate => Program.ReleaseDate;
    public string CommitHash => Program.CommitHash;

    public string Runtime => RuntimeInformation.FrameworkDescription;

    public string Platform =>
        $"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})";

    #endregion

    #region Links

    public string RepositoryUrl => Program.RepositoryUrl;
    public string DocumentationUrl => Program.DocumentationUrl;
    public string IssuesUrl => Program.IssuesUrl;
    public string LicenseUrl => Program.LicenseUrl;

    [RelayCommand]
    private Task OpenLinkAsync(string? url)
    {
        if (!string.IsNullOrEmpty(url) && Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return TopLevelHelper.LaunchUriAsync(
                TopLevel.GetTopLevel(this),
                uri,
                Properties.Resources.AboutModal_OpenLinkDangerNotificationTitle
            );
        }

        return Task.CompletedTask;
    }

    #endregion
}
