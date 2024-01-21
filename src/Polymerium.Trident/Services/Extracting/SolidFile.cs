namespace Polymerium.Trident.Services.Extracting;

public class SolidFile(string fileName, ReadOnlyMemory<byte> data)
{
    public string FileName { get; } = fileName;
    public ReadOnlyMemory<byte> Data { get; } = data;
}