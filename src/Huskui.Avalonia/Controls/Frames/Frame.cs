using System.Collections.Immutable;
using Avalonia.Controls.Primitives;

namespace Huskui.Avalonia.Controls.Frames;

public class Frame : TemplatedControl
{
    private readonly Stack<FrameFrame> _history = new Stack<FrameFrame>();
    public IEnumerable<FrameFrame> History => _history;

    public bool CanGoBack => _history.Count > 0;

    public void Navigate(Type page, object? parameter = null)
    {
    }

    public void GoBack()
    {
        if (_history.TryPop(out var frame))
        {
            Navigate(frame.Page, frame.Parameter);
        }
        else throw new InvalidOperationException("No previous page in the stack");
    }
}