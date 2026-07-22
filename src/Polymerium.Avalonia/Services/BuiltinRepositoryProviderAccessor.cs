using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Polymerium.Avalonia.Repositories;
using TridentCore.Core.Services;
using TridentCore.Core.Utilities;

namespace Polymerium.Avalonia.Services;

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
                                                                         null,
                                                                         ["edge.forgecdn.net", "media.forgecdn.net"]);

        var modrinth = new IRepositoryProviderAccessor.ProviderProfile(ModrinthHelper.LABEL,
                                                                       IRepositoryProviderAccessor.ProviderProfile
                                                                          .DriverType.Modrinth,
                                                                       ModrinthHelper.OFFICIAL_ENDPOINT,
                                                                       null,
                                                                       null);
        var packwiz = new IRepositoryProviderAccessor.ProviderProfile(PackwizHelper.LABEL,
                                                                      IRepositoryProviderAccessor.ProviderProfile
                                                                         .DriverType.Packwiz,
                                                                      "https://api.github.com/",
                                                                      null,
                                                                      null);


        // var bbsmc = new IRepositoryProviderAccessor.ProviderProfile("bbsmc",
        //                                                             IRepositoryProviderAccessor.ProviderProfile
        //                                                                .DriverType.Modrinth,
        //                                                             ModrinthHelper.FAKE_ENDPOINT,
        //                                                             null,
        //                                                             null);

        return [curseforge, modrinth, packwiz];
    }

    public IReadOnlyList<IRepositoryProviderAccessor.ProviderCustom> BuildCustom()
    {
        var favorite = ActivatorUtilities.CreateInstance<FavoriteRepository>(serviceProvider);

        return [new("favorite", favorite)];
    }

    #endregion
}
