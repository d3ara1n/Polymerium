namespace Polymerium.App.Facilities;

public class ViewBag(ViewBagFactory factory)
{
    public object? Parameter => factory.Bag;

    public bool IsEmpty => Parameter is null;
}