namespace Polymerium.Core.Models.CurseForge.Eternal;

public struct EternalModFileDependency
{
    public int ModId { get; set; }

    //1 = EmbeddedLibrary
    //2 = OptionalDependency
    //3 = RequiredDependency
    //4 = Tool
    //5 = Incompatible
    //6 = Include
    public int RelationType { get; set; }
}
