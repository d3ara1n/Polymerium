namespace Polymerium.Trident.Models.ModrinthApi;

public record MemberInfo(
    string TeamId,
    UserInfo User,
    string Role,
    bool IsOwner,
    uint? Permissions,
    uint? OrganizationPermissions,
    bool Accepted,
    int? PayoutSplit,
    int Ordering);