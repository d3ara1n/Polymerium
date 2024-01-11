namespace Polymerium.Trident.Models.Eternal;

public struct EternalDependency
{
    public uint ModId { get; set; }

    /// <summary>
    ///     1 = EmbeddedLibrary
    ///     2 = OptionalDependency
    ///     3 = RequiredDependency
    ///     4 = Tool
    ///     5 = Incompatible
    ///     6 = Include
    /// </summary>
    public uint RelationType { get; set; }
}