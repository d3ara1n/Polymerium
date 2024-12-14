using Avalonia.Collections;

namespace Huskui.Avalonia.Controls;

public class OverlayItems : AvaloniaList<OverlayItem>
{
    public override void Insert(int index, OverlayItem item)
    {
        base.Insert(index, item);


        for (var i = index; i >= 0; i--)
        {
            var v = this[i];
            var d = Count - i - 1;
            v.Distance = d;
        }
    }

    public override void RemoveAt(int index)
    {
        base.RemoveAt(index);

        for (var i = index - 1; i >= 0; i--)
        {
            var v = this[i];
            var d = Count - i - 1;
            v.Distance = d;
        }
    }

    public override bool Remove(OverlayItem item)
    {
        var rv = base.Remove(item);

        for (var i = Count - 1; i >= 0; i--)
        {
            var v = this[i];
            var d = Count - i - 1;
            v.Distance = d;
        }

        return rv;
    }

    public override void Add(OverlayItem item)
    {
        base.Add(item);

        for (var i = Count - 1; i >= 0; i--)
        {
            var v = this[i];
            var d = Count - i - 1;
            v.Distance = d;
        }
    }
}