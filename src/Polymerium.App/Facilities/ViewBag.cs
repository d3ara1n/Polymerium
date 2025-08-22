namespace Polymerium.App.Facilities;

public class ViewBag
{
    private readonly ViewBagFactory? _factory;
    private readonly object? _parameter;

    public ViewBag(ViewBagFactory factory) => _factory = factory;

    public ViewBag(object? parameter) => _parameter = parameter;

    public object? Parameter => _parameter ?? _factory?.Bag;

    public bool IsEmpty => Parameter is null;
}