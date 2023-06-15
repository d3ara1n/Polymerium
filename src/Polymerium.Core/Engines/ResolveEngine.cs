using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using DotNext;
using Duffet;
using Duffet.Builders;
using Polymerium.Abstractions.ResourceResolving;
using Polymerium.Abstractions.ResourceResolving.Attributes;
using Polymerium.Abstractions.Resources;

namespace Polymerium.Core.Engines;

public class ResolveEngine
{
    private readonly IEnumerable<ResolverTuple> _tuples;

    public ResolveEngine(IEnumerable<ResourceResolverBase> resolvers)
    {
        // 保证 DomainName 为 null 最后被匹配到
        _tuples = resolvers
            .SelectMany(GetTuplesInType)
            .OrderByDescending(x => x.DomainName?.Length ?? -1);
    }

    private IEnumerable<ResolverTuple> GetTuplesInType(ResourceResolverBase resolver)
    {
        var res = resolver.GetType();
        var domain = res.GetCustomAttribute<ResourceDomainAttribute>();
        var domainName = domain?.DomainName;
        var type = res.GetCustomAttribute<ResourceTypeAttribute>();
        var methods = res.GetMethods();
        foreach (var method in methods)
            // 这不是 HyperaiX.UnitBase，对返回值严格要求
            if (
                method.IsPublic
                && (
                    method.ReturnType == typeof(Result<ResolveResult, ResolveResultError>)
                    || method.ReturnType == typeof(Task<Result<ResolveResult, ResolveResultError>>)
                )
            )
            {
                var methodType = method.GetCustomAttribute<ResourceTypeAttribute>();
                var expression = method.GetCustomAttribute<ResourceExpressionAttribute>();
                if (expression != null)
                {
                    var realType = methodType ?? type;
                    if (realType != null)
                        yield return new ResolverTuple(
                            realType.ResourceType,
                            domainName,
                            method,
                            expression,
                            resolver
                        );
                }
            }
    }

    public async Task<Result<IEnumerable<ResolveResult>, ResolveResultError>> ResolveAsync(
        IEnumerable<Uri> resources,
        ResolverContext context
    )
    {
        var tasks = new List<Task<Result<ResolveResult, ResolveResultError>>>();
        foreach (var resource in resources)
        {
            tasks.Add(ResolveAsync(resource, context));
        }
        await Task.WhenAll(tasks);
        if (tasks.All(x => x.IsCompletedSuccessfully && x.Result.IsSuccessful))
        {
            return new Result<IEnumerable<ResolveResult>, ResolveResultError>(
                tasks.Select(x => x.Result.Value)
            );
        }
        else
        {
            return new Result<IEnumerable<ResolveResult>, ResolveResultError>(
                tasks.First(x => !x.Result.IsSuccessful).Result.Error
            );
        }
    }

    public async Task<Result<ResolveResult, ResolveResultError>> ResolveAsync(
        Uri resource,
        ResolverContext context
    )
    {
        if (resource.Scheme == "poly-res")
        {
            var type = resource.Host;
            var domain = !string.IsNullOrEmpty(resource.UserInfo) ? resource.UserInfo : null;
            var expression = resource.GetComponents(UriComponents.Path, UriFormat.Unescaped);
            var query = HttpUtility.ParseQueryString(resource.Query);
            var resolver = _tuples.FirstOrDefault(
                x =>
                    x.Type.ToString().Equals(type, StringComparison.OrdinalIgnoreCase)
                    && (
                        domain == null
                            ? x.DomainName == null
                            : x.DomainName == domain || x.DomainName == null
                    )
            );
            if (resolver != null)
            {
                // prepare path arguments
                var builder = new BankBuilder();
                var match = resolver.Expression.Compiled.Match(expression);
                if (match.Success)
                    foreach (Group group in match.Groups)
                        if (group.Success && group.Name != string.Empty)
                        {
                            var name = group.Name;
                            var value = group.Value;
                            builder.Property().Named(name).Typed(typeof(string)).WithObject(value);
                        }

                // prepare query arguments
                foreach (string key in query.Keys)
                    builder.Property().Named(key).Typed(typeof(string)).WithObject(query.Get(key));

                var bank = builder.Build();
                resolver.Self.Context = context;
                return await ExecuteAsAsyncStateMachine(resolver.Method, resolver.Self, bank);
            }

            return new Result<ResolveResult, ResolveResultError>(ResolveResultError.NotFound);
        }

        throw new ArgumentException("Scheme only accepts 'poly-res'", nameof(resource));
    }

    public async Task<Result<ResolveResult, ResolveResultError>> ResolveToFileAsync(
        Uri url,
        ResolverContext context
    )
    {
        var result = await ResolveAsync(url, context);
        if (result.IsSuccessful && result.Value.Type != ResourceType.File)
            return await ResolveAsync(result.Value.Resource.File!, context);
        return result;
    }

    public async Task<Result<ResolveResult, ResolveResultError>> ResolveToUpdateAsync(
        Uri url,
        ResolverContext context
    )
    {
        var result = await ResolveAsync(url, context);
        if (result.IsSuccessful && result.Value.Type != ResourceType.Update)
            return await ResolveAsync(result.Value.Resource.Update!, context);
        return result;
    }

    private Task<Result<ResolveResult, ResolveResultError>> ExecuteAsAsyncStateMachine(
        MethodInfo method,
        object subject,
        Bank bank
    )
    {
        var arguments = bank.Serve(method);
        if (method.GetCustomAttribute<AsyncStateMachineAttribute>() != null)
            return (Task<Result<ResolveResult, ResolveResultError>>)(
                method.Invoke(subject, arguments)
                ?? Task.FromResult(
                    new Result<ResolveResult, ResolveResultError>(ResolveResultError.Unknown)
                )
            );
        return Task.Run(
            () =>
                (Result<ResolveResult, ResolveResultError>?)method.Invoke(subject, arguments)
                ?? new Result<ResolveResult, ResolveResultError>(ResolveResultError.Unknown)
        );
    }

    private sealed record ResolverTuple(
        ResourceType Type,
        string? DomainName,
        MethodInfo Method,
        ResourceExpressionAttribute Expression,
        ResourceResolverBase Self
    );
}
