using System.Collections.Generic;
using TridentCore.Core.Services;

namespace Polymerium.App.Services;

public class UserRepositoryProviderAccessor : IRepositoryProviderAccessor
{
    #region IRepositoryProviderAccessor Members

    public IReadOnlyList<IRepositoryProviderAccessor.ProviderProfile> Build() => [];

    #endregion
}
