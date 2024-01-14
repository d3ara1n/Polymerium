using System;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Polymerium.App.ViewModels;
using Polymerium.Trident.Data;
using Polymerium.Trident.Services;
using Trident.Abstractions.Repositories;

namespace Polymerium.App.Extensions;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddViewModel<TViewModel>(this IServiceCollection services)
        where TViewModel : ViewModelBase
    {
        services.AddTransient<TViewModel>();
        return services;
    }

    public static IServiceCollection AddStore<T>(this IServiceCollection services,
        Func<PolymeriumContext, string> mappedTo)
        where T : class, new()
    {
        services.AddSingleton(provider =>
        {
            var ctx = provider.GetRequiredService<PolymeriumContext>();
            return new Store<T>(mappedTo(ctx));
        });
        return services;
    }

    public static IServiceCollection AddSerializationOptions(this IServiceCollection services,
        Action<JsonSerializerOptions> configure)
    {
        var options = new JsonSerializerOptions();
        configure(options);
        //options.MakeReadOnly();
        services.AddSingleton(options);
        return services;
    }

    public static IServiceCollection AddRepository<T>(this IServiceCollection services)
        where T : class, IRepository
    {
        services.AddTransient<IRepository, T>();
        return services;
    }
}