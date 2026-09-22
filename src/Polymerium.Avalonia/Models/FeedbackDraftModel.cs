using CommunityToolkit.Mvvm.ComponentModel;

namespace Polymerium.Avalonia.Models;

public partial class FeedbackDraftModel : ObservableObject
{
    [ObservableProperty]
    public partial FeedbackKind Kind { get; set; }

    [ObservableProperty]
    public partial string Message { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;
}
