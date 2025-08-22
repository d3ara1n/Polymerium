namespace Polymerium.Trident.Services;

public interface IRepositoryProviderAccessor
{
    IReadOnlyList<ProviderProfile> Build();

    #region Nested type: ProviderProfile

    record struct ProviderProfile(
        string Label,
        ProviderProfile.DriverType Driver,
        string Endpoint,
        (string Key, string Value)? AuthorizationHeader,
        string? UserAgent)
    {
        #region DriverType enum

        public enum DriverType { CurseForge, Modrinth, GitHub }

        #endregion
    }

    #endregion
}
