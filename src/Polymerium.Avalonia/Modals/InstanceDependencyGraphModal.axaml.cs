using CommunityToolkit.Mvvm.Input;
using Huskui.Avalonia.Controls;

namespace Polymerium.Avalonia.Modals;

public partial class InstanceDependencyGraphModal : Modal
{
    public InstanceDependencyGraphModal() => InitializeComponent();

    /// <summary>自适应（cover fit）：短维充满视口，长维溢出可滚动，缩放不致太小看得清。</summary>
    [RelayCommand]
    private void FitToContent() => ZoomView.FitToContent();

    /// <summary>全视图（contain fit）：整张图刚好全部进入视口，较短维充满、较长维留白居中。</summary>
    [RelayCommand]
    private void FitToAll() => ZoomView.FitToAll();
}
