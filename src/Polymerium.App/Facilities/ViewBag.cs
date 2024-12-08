namespace Polymerium.App.Facilities;

public class ViewBag(object? parameter)
{
    public object? Parameter => parameter;

    public bool IsEmpty => parameter is null;

    public static ViewBag Empty { get; } = new(null);
}