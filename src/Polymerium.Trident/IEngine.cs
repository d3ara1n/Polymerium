namespace Polymerium.Trident;

public interface IEngine<in TFuel, out TProd>: IEnumerable<TProd>
{
    public void SetContext(TFuel fuel);
}