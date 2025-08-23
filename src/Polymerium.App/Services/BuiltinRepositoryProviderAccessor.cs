using System.Collections.Generic;
using Polymerium.Trident.Services;
using Polymerium.Trident.Utilities;

namespace Polymerium.App.Services
{
    public class BuiltinRepositoryProviderAccessor : IRepositoryProviderAccessor
    {
        #region IRepositoryProviderAccessor Members

        public IReadOnlyList<IRepositoryProviderAccessor.ProviderProfile> Build()
        {
            var curseforge = new IRepositoryProviderAccessor.ProviderProfile("curseforge",
                                                                             IRepositoryProviderAccessor.ProviderProfile
                                                                                .DriverType.CurseForge,
                                                                             CurseForgeHelper.ENDPOINT,
                                                                             ("x-api-key", CurseForgeHelper.API_KEY),
                                                                             null);

            var officialModrinth = new IRepositoryProviderAccessor.ProviderProfile("modrinth",
                IRepositoryProviderAccessor.ProviderProfile.DriverType.Modrinth,
                ModrinthHelper.OFFICIAL_ENDPOINT,
                null,
                null);

            var fakeModrinth = new IRepositoryProviderAccessor.ProviderProfile("bbsmc",
                                                                               IRepositoryProviderAccessor
                                                                                  .ProviderProfile.DriverType.Modrinth,
                                                                               ModrinthHelper.FAKE_ENDPOINT,
                                                                               null,
                                                                               null);

            return [curseforge, officialModrinth, fakeModrinth];
        }

        #endregion
    }
}
