namespace Polymerium.Trident.Models.Labrinth;

public struct LabrinthTeamMember
{
    public string TeamId { get; set; }
    public LabrinthTeamUser User { get; set; }
    public string Role { get; set; }
    public int? Permissions { get; set; }
    public bool Accepted { get; set; }
    public int? PayoutsSplit { get; set; }
    public int Ordering { get; set; }
}