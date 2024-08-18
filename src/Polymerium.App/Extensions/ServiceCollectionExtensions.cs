using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Polymerium.Trident.Data;
using Polymerium.Trident.Services;
using System;
using System.Text.Json;
using Trident.Abstractions.Extractors;
using Trident.Abstractions.Repositories;

namespace Polymerium.App.Extensions;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddViewModel<TViewModel>(this IServiceCollection services)
        where TViewModel : ObservableObject
    {
        services.AddTransient<TViewModel>();
        return services;
    }

    public static IServiceCollection AddStore<T>(this IServiceCollection services,
        Func<TridentContext, string> mappedTo)
        where T : class, new()
    {
        services.AddSingleton(provider =>
        {
            var ctx = provider.GetRequiredService<TridentContext>();
            return new Store<T>(mappedTo(ctx));
        });
        return services;
    }

    public static IServiceCollection AddSerializationOptions(this IServiceCollection services,
        Action<JsonSerializerOptions> configure)
    {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
        configure(options);
        services.AddSingleton(options);
        return services;
    }

    public static IServiceCollection AddRepository<T>(this IServiceCollection services)
        where T : class, IRepository
    {
        services.AddTransient<IRepository, T>();
        return services;
    }

    public static IServiceCollection AddExtractor<T>(this IServiceCollection services)
        where T : class, IExtractor
    {
        services.AddTransient<IExtractor, T>();
        return services;
    }

    public static IServiceCollection AddEngine<TEngine>(this IServiceCollection services)
        where TEngine : class
    {
        services.AddTransient<TEngine>();
        return services;
    }
}