using System.Collections.Generic;
using Trident.Core.Services;
using Trident.Core.Utilities;

namespace Polymerium.App.Services
{
    public class BuiltinRepositoryProviderAccessor : IRepositoryProviderAccessor
    {
        #region IRepositoryProviderAccessor Members

        public IReadOnlyList<IRepositoryProviderAccessor.ProviderProfile> Build()
        {
            var curseforge = new IRepositoryProviderAccessor.ProviderProfile(CurseForgeHelper.LABEL,
                                                                             IRepositoryProviderAccessor.ProviderProfile
                                                                                .DriverType.CurseForge,
                                                                             CurseForgeHelper.ENDPOINT,
                                                                             ("x-api-key", CurseForgeHelper.API_KEY),
                                                                             null);

            var modrinth = new IRepositoryProviderAccessor.ProviderProfile(ModrinthHelper.LABEL,
                                                                           IRepositoryProviderAccessor.ProviderProfile
                                                                              .DriverType.Modrinth,
                                                                           ModrinthHelper.OFFICIAL_ENDPOINT,
                                                                           null,
                                                                           null);
            return [curseforge, modrinth];
        }

        #endregion
    }
}
