namespace Polymerium.Core.Models.CurseForge.Eternal;

public struct EternalModFileHash
{
    public string Value { get; set; }

    /// <summary>
    ///     1 = Sha1; 2 = MD5
    /// </summary>
    public int Algo { get; set; }
}