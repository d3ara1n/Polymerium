namespace Polymerium.Trident.Models.Eternal;

public struct EternalFileHash
{
    public string Value { get; set; }

    /// <summary>
    ///     1 = Sha1; 2 = MD5
    /// </summary>
    public int Algo { get; set; }
}