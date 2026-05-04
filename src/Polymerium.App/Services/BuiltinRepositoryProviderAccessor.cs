using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Polymerium.App.Repositories;
using TridentCore.Abstractions.Repositories;
using TridentCore.Core.Services;
using TridentCore.Core.Utilities;

namespace Polymerium.App.Services;

public class BuiltinRepositoryProviderAccessor(IServiceProvider serviceProvider) : IRepositoryProviderAccessor
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
        // var bbsmc = new IRepositoryProviderAccessor.ProviderProfile("bbsmc",
        //                                                             IRepositoryProviderAccessor.ProviderProfile
        //                                                                .DriverType.Modrinth,
        //                                                             ModrinthHelper.FAKE_ENDPOINT,
        //                                                             null,
        //                                                             null);
        return [curseforge, modrinth];
    }

    public IReadOnlyList<IRepositoryProviderAccessor.ProviderCustom> BuildCustom()
    {
        var favorite = ActivatorUtilities.CreateInstance<FavoriteRepository>(serviceProvider);

        return [new("favorite", favorite)];
    }

    #endregion
}
